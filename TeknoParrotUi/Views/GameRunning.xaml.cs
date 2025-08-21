using System;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using TeknoParrotUi.Common;
using TeknoParrotUi.Common.Jvs;
using TeknoParrotUi.Common.Pipes;
using TeknoParrotUi.Helpers;
using Linearstar.Windows.RawInput;
using TeknoParrotUi.Common.InputListening;
using System.Text;

namespace TeknoParrotUi.Views
{
    /// <summary>
    /// Interaction logic for GameRunningUC.xaml
    /// </summary>
    public partial class GameRunning
    {
        private readonly bool _isTest;
        private readonly string _gameLocation;
        private readonly string _gameLocation2;
        private readonly SerialPortHandler _serialPortHandler;
        private readonly GameProfile _gameProfile;
        private static bool _runEmuOnly;
        private static Thread _diThread;
        private static ControlSender _controlSender;
        private static readonly InputListener InputListener = new InputListener();
        private bool _forceQuit;
        private readonly bool _cmdLaunch;
        private static ControlPipe _pipe;
        private Library _library;
        private string loaderExe;
        private string loaderDll;
        private HwndSource _source;
        private InputApi _inputApi = InputApi.DirectInput;
        private bool _twoExes;
        private bool _secondExeFirst;
        private string _secondExeArguments;
        private string _gameVersion;
        private int _noMaxiTerminal;
#if DEBUG
        DebugJVS jvsDebug;
#endif

        public GameRunning(GameProfile gameProfile, string loaderExe, string loaderDll, bool isTest, bool runEmuOnly = false, bool profileLaunch = false, Library library = null)
        {
            InitializeComponent();
            if (profileLaunch == false && !runEmuOnly)
            {
                Application.Current.Windows.OfType<MainWindow>().Single().menuButton.IsEnabled = false;
            }

            // Get Input API
            string inputApiString = gameProfile.ConfigValues.Find(cv => cv.FieldName == "Input API")?.FieldValue;

            if (inputApiString != null)
                _inputApi = (InputApi)Enum.Parse(typeof(InputApi), inputApiString);

            // Check run MaxiTerminal or not
            _noMaxiTerminal = int.Parse(gameProfile.ConfigValues.Find(cv => cv.FieldName == "Don't Run MaxiTerminal")?.FieldValue);
            
            // Check again if it's Terminal Mode or not
            if(_noMaxiTerminal == 0)
            {
                _noMaxiTerminal = int.Parse(gameProfile.ConfigValues.Find(cv => cv.FieldName == "TerminalMode")?.FieldValue);
            }

            textBoxConsole.Text = "";
            _runEmuOnly = runEmuOnly;
            _gameLocation = gameProfile.GamePath;
            _gameLocation2 = gameProfile.GamePath2;
            _gameVersion = gameProfile.GameVersion;
            _twoExes = gameProfile.HasTwoExecutables;
            _secondExeFirst = gameProfile.LaunchSecondExecutableFirst;
            _secondExeArguments = gameProfile.SecondExecutableArguments;
            InputCode.ButtonMode = gameProfile.EmulationProfile;
            _isTest = isTest;
            _gameProfile = gameProfile;
            _serialPortHandler = new SerialPortHandler();
            _cmdLaunch = profileLaunch;

            if (!_isTest)
            {
                if (_inputApi == InputApi.XInput)
                {
                    if (InputListenerXInput.DisableTestButton)
                    {
                        InputListenerXInput.DisableTestButton = false;
                    }
                }
                else if (_inputApi == InputApi.DirectInput)
                {
                    if (InputListenerDirectInput.DisableTestButton)
                    {
                        InputListenerDirectInput.DisableTestButton = false;
                    }
                }
            }

            if (runEmuOnly)
            {
                buttonForceQuit.Visibility = Visibility.Collapsed;
            }

            gameName.Text = _gameProfile.GameName;
            _library = library;
            this.loaderExe = loaderExe;
            this.loaderDll = loaderDll;
#if DEBUG
            jvsDebug = new DebugJVS();
            jvsDebug.Show();
#endif
        }

        private void WriteConfigIni()
        {
            var lameFile = "";
            var categories = _gameProfile.ConfigValues.Select(x => x.CategoryName).Distinct().ToList();
            lameFile += "[GlobalHotkeys]\n";
            lameFile += "ExitKey=" + Lazydata.ParrotData.ExitGameKey + "\n";
            lameFile += "PauseKey=" + Lazydata.ParrotData.PauseGameKey + "\n";

            if (_twoExes && !string.IsNullOrEmpty(_gameLocation2))
            {
                var Paths = Path.Combine(Path.GetDirectoryName(_gameLocation2), "AMConfig.ini");
                var MyIni = new IniReader(@Paths);
                var amucfg_game_rev = MyIni.GetValue("amucfg-game_rev", "AMUpdaterConfig");
                var cacfg_game_ver = MyIni.GetValue("cacfg-game_ver", "MuchaCAConfig");
                bool check5DX00Revision = cacfg_game_ver.Contains("00.");

                //var charsToRemove = ".";
                //cacfg_game_ver = cacfg_game_ver.Replace(charsToRemove, string.Empty);
                //StringBuilder strB = new StringBuilder(cacfg_game_ver);

                if (_gameVersion == "W5X10JPN" && check5DX00Revision)
                {
                    amucfg_game_rev = "1";
                }

                lameFile += "[Version]\n";
                lameFile += "GameRevision=" + amucfg_game_rev + "." + cacfg_game_ver + "\n";
            }

            if (!string.IsNullOrWhiteSpace(_gameVersion))
            { 
                lameFile += "GameVersion=" + _gameVersion + "\n";
            }


            for (var i = 0; i < categories.Count(); i++)
            {
                lameFile += $"[{categories[i]}]{Environment.NewLine}";
                var variables = _gameProfile.ConfigValues.Where(x => x.CategoryName == categories[i]);
                lameFile = variables.Aggregate(lameFile,
                    (current, fieldInformation) =>
                        current + $"{fieldInformation.FieldName}={fieldInformation.FieldValue}{Environment.NewLine}");
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(_gameLocation) ?? throw new InvalidOperationException(), "teknoparrot.ini"), lameFile);

            if (_twoExes && !string.IsNullOrEmpty(_gameLocation2))
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(_gameLocation2) ?? throw new InvalidOperationException(), "teknoparrot.ini"), lameFile);
            }
        }
        
        private void GameRunning_OnLoaded(object sender, RoutedEventArgs e)
        {
            JvsPackageEmulator.Initialize();
            _pipe?.Start(_runEmuOnly);
            _controlSender?.Start();

            if (!_runEmuOnly)
                WriteConfigIni();

            if (InputCode.ButtonMode == EmulationProfile.NamcoWmmt5)
            {
                //bool DualJvsEmulation = _gameProfile.ConfigValues.Any(x => x.FieldName == "DualJvsEmulation" && x.FieldValue == "1");

                // TODO: MAYBE MAKE THESE XML BASED?
                JvsPackageEmulator.JvsVersion = 0x31;
                JvsPackageEmulator.JvsCommVersion = 0x31;
                JvsPackageEmulator.JvsCommandRevision = 0x31;
                JvsPackageEmulator.JvsIdentifier = JVSIdentifiers.NBGI_MarioKart3;
                JvsPackageEmulator.Namco = true;
                JvsPackageEmulator.JvsSwitchCount = 0x18;

                _serialPortHandler.StopListening();
                Thread.Sleep(1000);
                new Thread(() => _serialPortHandler.ListenPipe("TeknoParrot_JVS")).Start();
                new Thread(_serialPortHandler.ProcessQueue).Start();
            }

            _diThread?.Abort(0);
            _diThread = CreateInputListenerThread();

            if (Lazydata.ParrotData.UseDiscordRPC)
            {
                DiscordRPC.UpdatePresence(new DiscordRPC.RichPresence
                {
                    details = _gameProfile.GameName,
                    state = "Playing",
                    largeImageKey = _gameProfile.GameName.Replace(" ", "").ToLower(),

                    // Timestamp sc: https://stackoverflow.com/a/17632585
                    startTimestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
                });
            }

            // Wait before launching second thread.
            if (!_runEmuOnly)
            {
                Thread.Sleep(1000);
                CreateGameProcess();
            }
            else
            {
#if DEBUG
                if (jvsDebug != null)
                {
                    new Thread(() =>
                    {
                        while (true)
                        {
                            if (jvsDebug.JvsOverride)
                                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(jvsDebug.DoCheckBoxesDude));
                        }
                    }).Start();
                }
#endif
            }
        }

        public static IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        private void CreateGameProcess()
        {
            var gameThread = new Thread(() =>
            {
                var windowed = _gameProfile.ConfigValues.Any(x => x.FieldName == "Windowed" && x.FieldValue == "1") || _gameProfile.ConfigValues.Any(x => x.FieldName == "DisplayMode" && x.FieldValue == "Windowed");
                var fullscreen = _gameProfile.ConfigValues.Any(x => x.FieldName == "Windowed" && x.FieldValue == "0") || _gameProfile.ConfigValues.Any(x => x.FieldName == "DisplayMode" && x.FieldValue == "Fullscreen");
                var width = _gameProfile.ConfigValues.FirstOrDefault(x => x.FieldName == "ResolutionWidth");
                var height = _gameProfile.ConfigValues.FirstOrDefault(x => x.FieldName == "ResolutionHeight");

                var custom = string.Empty;
                if (!string.IsNullOrEmpty(_gameProfile.CustomArguments))
                {
                    custom = _gameProfile.CustomArguments;
                }

                var extra_xml = string.Empty;
                if (!string.IsNullOrEmpty(_gameProfile.ExtraParameters))
                {
                    extra_xml = _gameProfile.ExtraParameters;
                }

                // TODO: move to XML
                var extra = string.Empty;
                string gameArguments = $"\"{_gameLocation}\" {extra} {custom} {extra_xml}";

                if (_gameProfile.ResetHint)
                {
                    var hintPath = Path.Combine(Path.GetDirectoryName(_gameProfile.GamePath), "hints.dat");
                    if (File.Exists(hintPath))
                    {
                        File.Delete(hintPath);
                    }
                }

                ProcessStartInfo info = new ProcessStartInfo(loaderExe, $"{loaderDll} {gameArguments}");
                info.UseShellExecute = false;
                info.WindowStyle = ProcessWindowStyle.Normal;

                if (_gameProfile.msysType > 0)
                {
                    info.EnvironmentVariables.Add("tp_msysType", _gameProfile.msysType.ToString());
                }

                if (InputCode.ButtonMode == EmulationProfile.NamcoWmmt5)
                {
                    var amcus = Path.Combine(Path.GetDirectoryName(_gameLocation), "AMCUS");

                    //If these files exist, this isn't a "original version"
                    if (File.Exists(Path.Combine(amcus, "AMAuthd.exe")) &&
                        File.Exists(Path.Combine(amcus, "iauthdll.dll")))
                    {
                        // Register iauthd.dll
                        Register_Dlls(Path.Combine(Path.GetDirectoryName(_gameLocation), "AMCUS", "iauthdll.dll"));

                        // Start AMCUS
                        RunAndWait(loaderExe,
                            $"{loaderDll} \"{Path.Combine(Path.GetDirectoryName(_gameLocation), "AMCUS", "AMAuthd.exe")}\"");
                    }
                }

                var cmdProcess = new Process
                {
                    StartInfo = info
                };

                cmdProcess.OutputDataReceived += (sender, e) =>
                {
                    // Prepend line numbers to each line of the output.
                    if (string.IsNullOrEmpty(e.Data)) return;
                    textBoxConsole.Dispatcher.Invoke(() => textBoxConsole.Text += "\n" + e.Data,
                        DispatcherPriority.Background);
                    Console.WriteLine(e.Data);
                };

                cmdProcess.EnableRaisingEvents = true;

                cmdProcess.Start();

                //cmdProcess.WaitForExit();
                while (!cmdProcess.HasExited)
                {
#if DEBUG
                    if (jvsDebug != null)
                    {
                        if (jvsDebug.JvsOverride)
                            Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(jvsDebug.DoCheckBoxesDude));
                    }
#endif
                    if (_forceQuit)
                    {
                        cmdProcess.Kill();
                    }

                    Thread.Sleep(500);
                }
                
                TerminateThreads();
                if (_runEmuOnly || _cmdLaunch)
                {
                    Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                }
                else if (_forceQuit == false)
                {
                    textBoxConsole.Invoke(delegate
                    {
                        gameRunning.Text = Properties.Resources.GameRunningGameStopped;
                        progressBar.IsIndeterminate = false;
                        Application.Current.Windows.OfType<MainWindow>().Single().menuButton.IsEnabled = true;
                    });
                    Application.Current.Dispatcher.Invoke(delegate
                        {
                            Application.Current.Windows.OfType<MainWindow>().Single().contentControl.Content = _library;
                        });
                }
                else
                {
                    textBoxConsole.Invoke(delegate
                    {
                        gameRunning.Text = Properties.Resources.GameRunningGameStopped;
                        progressBar.IsIndeterminate = false;
                        MessageBoxHelper.WarningOK(Properties.Resources.GameRunningCheckTaskMgr);
                        Application.Current.Windows.OfType<MainWindow>().Single().menuButton.IsEnabled = true;
                    });
                }
            });
            gameThread.Start();
        }

        private static void Register_Dlls(string filePath)
        {
            try
            {
                //'/s' : Specifies regsvr32 to run silently and to not display any message boxes.
                string argFileinfo = "/s" + " " + "\"" + filePath + "\"";
                Process reg = new Process();
                //This file registers .dll files as command components in the registry.
                reg.StartInfo.FileName = "regsvr32.exe";
                reg.StartInfo.Arguments = argFileinfo;
                reg.StartInfo.UseShellExecute = false;
                reg.StartInfo.CreateNoWindow = true;
                reg.StartInfo.RedirectStandardOutput = true;
                reg.Start();
                reg.WaitForExit();
                reg.Close();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ErrorOK(ex.ToString());
            }
        }

        // Wait before running 2nd exe
        private void RunAndWait(string loaderExe, string daemonPath)
        {
            Process.Start(new ProcessStartInfo(loaderExe, daemonPath));
            Thread.Sleep(1000);
        }

        private Thread CreateInputListenerThread()
        {
            var hWnd = new WindowInteropHelper(Application.Current.MainWindow ?? throw new InvalidOperationException()).EnsureHandle();
            var inputThread = new Thread(() => InputListener.Listen(Lazydata.ParrotData.UseSto0ZDrivingHack, Lazydata.ParrotData.StoozPercent, _gameProfile.JoystickButtons, _inputApi, _gameProfile));
            inputThread.Start();

            // Hook window proc messages
            if (_inputApi == InputApi.RawInput)
            {
                RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, hWnd);
                RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, hWnd);

                _source = HwndSource.FromHwnd(hWnd);
                _source.AddHook(WndProcHook);
            }

            return inputThread;
        }

        private static IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            InputListener?.WndProcReceived(hwnd, msg, wParam, lParam, ref handled);
            return IntPtr.Zero;
        }

        private void TerminateThreads()
        {
            _controlSender?.Stop();
            InputListener?.StopListening();

            if (_inputApi == InputApi.RawInput)
            {
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
                _source?.RemoveHook(WndProcHook);
            }

            _serialPortHandler?.StopListening();
            _pipe?.Stop();
        }

        private void ButtonForceQuit_Click(object sender, RoutedEventArgs e)
        {
            _forceQuit = true;
        }

        public void GameRunning_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Lazydata.ParrotData.UseDiscordRPC) DiscordRPC.UpdatePresence(null);
#if DEBUG
            jvsDebug?.Close();
#endif
            TerminateThreads();
            Thread.Sleep(100);
            if (_runEmuOnly)
            {
                MainWindow.SafeExit();
            }
        }
    }
}
 