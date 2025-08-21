using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TeknoParrotUi.Common;
using TeknoParrotUi.Views;
using Application = System.Windows.Application;

namespace TeknoParrotUi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Library _library;
        private readonly AddGame _addGame;
        private bool _showingDialog;
        private bool _allowClose;
        public bool _updaterComplete = false;

        public MainWindow()
        {
            redistAndDirectXCheck();
            InitializeComponent();
            Directory.CreateDirectory("Icons");
            _library = new Library(contentControl);
            _addGame = new AddGame(contentControl, _library);
            contentControl.Content = _library;
            Title = "TeknoParrot UI " + GameVersion.CurrentVersion;

            SaveCompleteSnackbar.VerticalAlignment = VerticalAlignment.Top;
            SaveCompleteSnackbar.HorizontalContentAlignment = HorizontalAlignment.Center;
            // 2 seconds
            SaveCompleteSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(2000));
        }

        //this is a WIP, not working yet
        public void redistAndDirectXCheck()
        {
            string systemDir = Environment.SystemDirectory; // System32
            string sysWOW64Dir = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86); // SysWOW64

            // Start assuming everything is installed
            bool hasRedist = true;
            bool hasDirectX = true;

            string[] redistFiles =
            {
                "concrt140.dll","mfc100.dll","mfc100chs.dll","mfc100cht.dll","mfc100deu.dll","mfc100enu.dll","mfc100esn.dll",
                "mfc100fra.dll","mfc100ita.dll","mfc100jpn.dll","mfc100kor.dll","mfc100rus.dll","mfc100u.dll","mfc110.dll",
                "mfc110chs.dll","mfc110cht.dll","mfc110deu.dll","mfc110enu.dll","mfc110esn.dll","mfc110fra.dll","mfc110ita.dll",
                "mfc110jpn.dll","mfc110kor.dll","mfc110rus.dll","mfc110u.dll","mfc120.dll","mfc120chs.dll","mfc120cht.dll",
                "mfc120deu.dll","mfc120enu.dll","mfc120esn.dll","mfc120fra.dll","mfc120ita.dll","mfc120jpn.dll","mfc120kor.dll",
                "mfc120rus.dll","mfc120u.dll","mfc140.dll","mfc140chs.dll","mfc140cht.dll","mfc140deu.dll","mfc140enu.dll",
                "mfc140esn.dll","mfc140fra.dll","mfc140ita.dll","mfc140jpn.dll","mfc140kor.dll","mfc140rus.dll","mfc140u.dll",
                "mfc42.dll","mfc42u.dll","MFCaptureEngine.dll","mfcm100.dll","mfcm100u.dll","mfcm110.dll","mfcm110u.dll",
                "mfcm120.dll","mfcm120u.dll","mfcm140.dll","mfcm140u.dll","mfcore.dll","mfcsubs.dll","msvcirt.dll","msvcp_win.dll",
                "msvcp100.dll","msvcp110.dll","msvcp110_win.dll","msvcp120.dll","msvcp120_clr0400.dll","msvcp140.dll","msvcp140_1.dll",
                "msvcp140_2.dll","msvcp140_atomic_wait.dll","msvcp140_clr0400.dll","msvcp140_codecvt_ids.dll","msvcp140d_atomic_wait.dll",
                "msvcp140d_codecvt_ids.dll","msvcp60.dll","msvcr100.dll","msvcr100_clr0400.dll","msvcr110.dll","msvcr120.dll",
                "msvcr120_clr0400.dll","msvcrt.dll","vcamp110.dll","vcamp120.dll","vcamp140.dll","VCardParser.dll","vccorlib110.dll",
                "vccorlib120.dll","vccorlib140.dll","vcomp100.dll","vcomp110.dll","vcomp120.dll","vcomp140.dll","vcruntime140.dll",
                "vcruntime140_clr0400.dll","vcruntime140_threads.dll"
            };

            string[] directXFiles =
            {
                "d3dcompiler_33.dll","d3dcompiler_34.dll","d3dcompiler_35.dll","d3dcompiler_36.dll","D3DCompiler_37.dll",
                "D3DCompiler_38.dll","D3DCompiler_39.dll","D3DCompiler_40.dll","D3DCompiler_41.dll","D3DCompiler_42.dll",
                "D3DCompiler_43.dll","d3dcsx_42.dll","d3dcsx_43.dll","d3dx10.dll","d3dx10_33.dll","d3dx10_34.dll",
                "d3dx10_35.dll","d3dx10_36.dll","d3dx10_37.dll","d3dx10_38.dll","d3dx10_39.dll","d3dx10_40.dll","d3dx10_41.dll",
                "d3dx10_42.dll","d3dx10_43.dll","d3dx11_42.dll","d3dx11_43.dll","d3dx9_24.dll","d3dx9_25.dll","d3dx9_26.dll",
                "d3dx9_27.dll","d3dx9_28.dll","d3dx9_29.dll","d3dx9_30.dll","d3dx9_31.dll","d3dx9_32.dll","d3dx9_33.dll",
                "d3dx9_34.dll","d3dx9_35.dll","d3dx9_36.dll","d3dx9_37.dll","d3dx9_38.dll","d3dx9_39.dll","d3dx9_40.dll",
                "d3dx9_41.dll","d3dx9_42.dll","d3dx9_43.dll","x3daudio1_0.dll","x3daudio1_1.dll","x3daudio1_2.dll",
                "X3DAudio1_3.dll","X3DAudio1_4.dll","X3DAudio1_5.dll","X3DAudio1_6.dll","X3DAudio1_7.dll","xactengine2_0.dll",
                "xactengine2_1.dll","xactengine2_10.dll","xactengine2_2.dll","xactengine2_3.dll","xactengine2_4.dll",
                "xactengine2_5.dll","xactengine2_6.dll","xactengine2_7.dll","xactengine2_8.dll","xactengine2_9.dll","xactengine3_0.dll",
                "xactengine3_1.dll","xactengine3_2.dll","xactengine3_3.dll","xactengine3_4.dll","xactengine3_5.dll","xactengine3_6.dll",
                "xactengine3_7.dll","XAPOFX1_0.dll","XAPOFX1_1.dll","XAPOFX1_2.dll","XAPOFX1_3.dll","XAPOFX1_4.dll","XAPOFX1_5.dll",
                "XAudio2_0.dll","XAudio2_1.dll","XAudio2_2.dll","XAudio2_3.dll","XAudio2_4.dll","XAudio2_5.dll","XAudio2_6.dll","XAudio2_7.dll",
                "xinput1_1.dll","xinput1_2.dll","xinput1_3.dll","xinput9_1_0.dll"
            };

            if (Environment.Is64BitOperatingSystem)
            {
                foreach (var dll in redistFiles)
                {
                    if (!File.Exists(Path.Combine(sysWOW64Dir, dll)))
                    {
                        hasRedist = false;
                        break; // No need to keep checking
                    }
                }

                foreach (var dll in directXFiles)
                {
                    if (!File.Exists(Path.Combine(sysWOW64Dir, dll)))
                    {
                        hasDirectX = false;
                        break;
                    }
                }
            }
            else
            {
                foreach (var dll in redistFiles)
                {
                    if (!File.Exists(Path.Combine(systemDir, dll)))
                    {
                        hasRedist = false;
                        break; // No need to keep checking
                    }
                }

                foreach (var dll in directXFiles)
                {
                    if (!File.Exists(Path.Combine(systemDir, dll)))
                    {
                        hasDirectX = false;
                        break;
                    }
                }
            }

            if (!hasRedist || !hasDirectX)
            { 
                if (MessageBox.Show(
                    "It appears that your system is currently missing Visual C++ Redistributable Runtimes and/or DirectX.\n\n" +
                    "It is highly recommended that you install them for maximum compatibility with games.\n\n" +
                    "Would you like the program to download and install them for you?\n\n" + 
                    "Press NO does not guarantee your game can run",
                    "Missing Dependencies",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Debug.WriteLine("User chose YES ¡ª starting download/install process...");
                    string commands = @"
                        @echo off
                        cd /d ""%TEMP%""
                        echo Downloading DirectX...
                        powershell -NoLogo -NoProfile -Command ""Invoke-WebRequest -Uri 'https://download.microsoft.com/download/1/7/0/17098FC8-4B77-4F3F-BEA3-9EAD6B4B6022/directx_Jun2010_redist.exe' -OutFile 'directx.exe'""
                        echo Downloading Visual C++ AIO...
                        powershell -NoLogo -NoProfile -Command ""Invoke-WebRequest -Uri 'https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe' -OutFile 'vcredist_aio.exe'""
                        echo Installing DirectX...
                        directx.exe /Q
                        echo Installing Visual C++ Redistributables...
                        vcredist_aio.exe /y
                        echo Delete downloaded files...
                        del directx.exe /Q
                        del vcredist_aio.exe /Q
                        echo Done.
                        pause
                        ";

                    // Save to a temporary batch file so CMD runs everything cleanly
                    string batchPath = Path.Combine(Path.GetTempPath(), "install_deps.bat");
                    File.WriteAllText(batchPath, commands);

                    // Run the batch file and wait until it finishes
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C \"" + batchPath + "\"",
                        CreateNoWindow = false,
                        UseShellExecute = false
                    })?.WaitForExit();
                    File.Delete(batchPath);
                }
                else
                {
                    Debug.WriteLine("User chose NO ¡ª skipping install.");
                }
            }
        }

        public void ShowMessage(string message)
        {
            SaveCompleteSnackbar.MessageQueue.Enqueue(message);
        }

        /// <summary>
        /// Loads the library screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnLibrary(object sender, RoutedEventArgs e)
        {
            contentControl.Content = _library;
        }

        /// <summary>
        /// Shuts down the Discord integration then quits the program, terminating any threads that may still be running.
        /// </summary>
        public static void SafeExit()
        {
            if (Lazydata.ParrotData.UseDiscordRPC)
                DiscordRPC.Shutdown();

            Environment.Exit(0);
        }

        /// <summary>
        /// Loads the settings screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSettings(object sender, RoutedEventArgs e)
        {
            //_settingsWindow.ShowDialog();
            var settings = new UserControls.SettingsControl(contentControl, _library);
            contentControl.Content = settings;
        }

        StackPanel ConfirmExit()
        {
            var txt1 = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Lazydata.ParrotData.UiDarkMode ? "#FFFFFF" : "#303030")),
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.WrapWithOverflow,
                FontSize = 18,
                Text = Properties.Resources.MainAreYouSure
            };

            var dck = new DockPanel();
            dck.Children.Add(new Button()
            {
                Style = Application.Current.FindResource("MaterialDesignFlatButton") as Style,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Lazydata.ParrotData.UiDarkMode ? "#FFFFFF" : "#303030")),
                Width = 115,
                Height = 30,
                Margin = new Thickness(5),
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = true,
                Content = Properties.Resources.Yes
            });
            dck.Children.Add(new Button()
            {
                Style = Application.Current.FindResource("MaterialDesignFlatButton") as Style,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Lazydata.ParrotData.UiDarkMode ? "#FFFFFF" : "#303030")),
                Width = 115,
                Height = 30,
                Margin = new Thickness(5),
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = false,
                Content = Properties.Resources.No
            });

            var stk = new StackPanel
            {
                Width = 250,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Lazydata.ParrotData.UiDarkMode ? "#303030" : "#FFFFFF"))
            };
            stk.Children.Add(txt1);
            stk.Children.Add(dck);
            return stk;
        }

        /// <summary>
        /// If the window is being closed, prompts whether the user really wants to do that so it can safely shut down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            //If the user has elected to allow the close, simply let the closing event happen.
            if (_allowClose) return;

            //NB: Because we are making an async call we need to cancel the closing event
            e.Cancel = true;

            //we are already showing the dialog, ignore
            if (_showingDialog) return;

            if (Lazydata.ParrotData.ConfirmExit)
            {
                //Set flag indicating that the dialog is being shown
                _showingDialog = true;
                var result = await DialogHost.Show(ConfirmExit());
                _showingDialog = false;
                //The result returned will come form the button's CommandParameter.
                //If the user clicked "Yes" set the _AllowClose flag, and re-trigger the window Close.
                if (!(result is bool boolResult) || !boolResult) return;
            }

            _allowClose = true;
            _library.Joystick.StopListening();
            SafeExit();
        }

        /// <summary>
        /// Same as window_closed except on the quit button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnQuit(object sender, RoutedEventArgs e)
        {
            //If the user has elected to allow the close, simply let the closing event happen.
            if (_allowClose || _showingDialog) return;

            if (Lazydata.ParrotData.ConfirmExit)
            {
                //Set flag indicating that the dialog is being shown
                _showingDialog = true;
                var result = await DialogHost.Show(ConfirmExit());
                _showingDialog = false;
                //The result returned will come form the button's CommandParameter.
                //If the user clicked "Yes" set the _AllowClose flag, and re-trigger the window Close.
                if (!(result is bool boolResult) || !boolResult) return;
            }

            _allowClose = true;
            _library.Joystick.StopListening();
            SafeExit();
        }

        public class UpdaterComponent
        {
            // component name
            public string name { get; set; }
            // location of file to check version from, i.e TeknoParrot\TeknoParrot.dll
            public string location { get; set; }
            // repository name, if not set it will use name as the repo name
            public string reponame { get; set; }
            // if set, the changelog button will link to the commits page, if not it will link to the release directly
            public bool opensource { get; set; } = true;
            // if set, the updater will extract the files into this folder rather than the name folder
            public string folderOverride { get; set; }
            // if set, it will grab the update from a specific github user's account, if not set it'll use teknogods
            public string userName { get; set; }
            public string fullUrl { get { return "https://github.com/" + (!string.IsNullOrEmpty(userName) ? userName : "teknogods") + "/" + (!string.IsNullOrEmpty(reponame) ? reponame : name) + "/"; }
            }
            // if set, this will write the version to a text file when extracted then refer to that when checking.
            public bool manualVersion { get; set; } = false;
            // local version number
            public string _localVersion;
            public string localVersion
            {
                get
                {
                    if (_localVersion == null)
                    {
                        if (File.Exists(location))
                        {
                            if (manualVersion)
                            {
                                if (File.Exists(Path.GetDirectoryName(location) + "\\.version"))
                                    _localVersion = File.ReadAllText(Path.GetDirectoryName(location) + "\\.version");
                                else
                                    _localVersion = "unknown";
                            }
                            else
                            {
                                var fvi = FileVersionInfo.GetVersionInfo(location);
                                var pv = fvi.ProductVersion;
                                _localVersion = (fvi != null && pv != null) ? pv : "unknown";
                            }
                        }
                        else
                        {
                            _localVersion = Properties.Resources.UpdaterNotInstalled;
                        }
                    }

                    return _localVersion;
                }
            }
        }

        public static List<UpdaterComponent> components = new List<UpdaterComponent>()
        {
            new UpdaterComponent
            {
                name = "TeknoParrotUI",
                location = Assembly.GetExecutingAssembly().Location
            },
            new UpdaterComponent
            {
                name = "OpenParrotWin32",
                location = Path.Combine("OpenParrotWin32", "OpenParrot.dll"),
                reponame = "OpenParrot"
            },
            new UpdaterComponent
            {
                name = "OpenParrotx64",
                location = Path.Combine("OpenParrotx64", "OpenParrot64.dll"),
                reponame = "OpenParrot"
            },
            new UpdaterComponent
            {
                name = "OpenSegaAPI",
                location = Path.Combine("TeknoParrot", "Opensegaapi.dll"),
                folderOverride = "TeknoParrot"
            },
            new UpdaterComponent
            {
                name = "TeknoParrot",
                location = Path.Combine("TeknoParrot", "TeknoParrot.dll"),
                opensource = false
            },
            new UpdaterComponent
            {
                name = "TeknoParrotN2",
                location = Path.Combine("N2", "TeknoParrot.dll"),
                reponame = "TeknoParrot",
                opensource = false,
                folderOverride = "N2"
            },
            new UpdaterComponent
            {
                name = "SegaTools",
                location = Path.Combine("SegaTools", "idzhook.dll"),
                reponame = "SegaToolsTP",
                folderOverride = "SegaTools",
                userName = "nzgamer41"
            },
            new UpdaterComponent
            {
                name = "OpenSndGaelco",
                location = Path.Combine("TeknoParrot", "OpenSndGaelco.dll"),
                folderOverride = "TeknoParrot"
            },
            new UpdaterComponent
            {
                name = "OpenSndVoyager",
                location = Path.Combine("TeknoParrot", "OpenSndVoyager.dll"),
                folderOverride = "TeknoParrot"
            },
            new UpdaterComponent
            {
                name = "ScoreSubmission",
                location = Path.Combine("TeknoParrot", "ScoreSubmission.dll"),
                folderOverride = "TeknoParrot",
                userName = "Boomslangnz"
            },
            new UpdaterComponent
            {
                name = "TeknoParrotElfLdr2",
                location = Path.Combine("ElfLdr2", "TeknoParrot.dll"),
                reponame = "TeknoParrot",
                opensource = false,
                manualVersion = true,
                folderOverride = "ElfLdr2"            
            }
        };

        async Task<GithubRelease> GetGithubRelease(UpdaterComponent component)
        {
            using (var client = new HttpClient())
            {
#if DEBUG
                //https://github.com/settings/applications/new GET ONE HERE
                //MAKE SURE YOU DO NOT COMMIT THIS TOKEN IF YOU ADD IT! ONLY USE FOR DEVELOPMENT THEN REMOVE!
                //(bypasses retarded rate limit)            
                string secret = string.Empty; //?client_id=CLIENT_ID_HERE&client_secret=CLIENT_SECRET_HERE"
#else
                string secret = string.Empty;
#endif
                //Github's API requires a user agent header, it'll 403 without it
                client.DefaultRequestHeaders.Add("User-Agent", "TeknoParrot");
                var reponame = !string.IsNullOrEmpty(component.reponame) ? component.reponame : component.name;
                var url = $"https://api.github.com/repos/{(!string.IsNullOrEmpty(component.userName) ? component.userName : "teknogods")}/{reponame}/releases/tags/{component.name}{secret}";
                Debug.WriteLine($"Updater url for {component.name}: {url}");
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var release = await response.Content.ReadAsAsync<GithubRelease>();
                    return release;
                }
                else
                {
                    // Handle github exceptions nicely
                    string message = "Unkown exception";
                    string mediaType = response.Content.Headers.ContentType.MediaType;
                    string body = await response.Content.ReadAsStringAsync();
                    HttpStatusCode statusCode = response.StatusCode;

                    if (statusCode == HttpStatusCode.NotFound)
                    {
                        message = "Not found!";
                    }
                    else if (mediaType == "text/html")
                    {
                        message = body.Trim();
                    }
                    else if (mediaType == "application/json")
                    {
                        var json = JObject.Parse(body);
                        message = json["message"]?.ToString();

                        if (message.Contains("API rate limit exceeded"))
                            message = "Update limit exceeded, try again in an hour!";
                    }

                    throw new Exception(message);
                }
            }
        }

        public int GetVersionNumber(string version)
        {
            var split = version.Split('.');
            if (split.Length != 4 || string.IsNullOrEmpty(split[3]) || !int.TryParse(split[3], out var ver))
            {
                Debug.WriteLine($"{version} is formatted incorrectly!");
                return 0;
            }
            return ver;
        }

        private async Task CheckGithub(UpdaterComponent component)
        {
            try
            {
                var githubRelease = await GetGithubRelease(component);
                if (githubRelease != null && githubRelease.assets != null && githubRelease.assets.Count != 0)
                {
                    var localVersionString = component.localVersion;
                    var onlineVersionString = githubRelease.name;
                    // fix for weird things like OpenParrotx64_1.0.0.30
                    if (onlineVersionString.Contains(component.name))
                    {
                        onlineVersionString = onlineVersionString.Split('_')[1];
                    }

                    bool needsUpdate = false;
                    // component not installed.
                    if (localVersionString == Properties.Resources.UpdaterNotInstalled)
                    {
                        needsUpdate = true;
                    }
                    else
                    {
                        switch (localVersionString)
                        {
                            // version number is weird / unable to be formatted
                            case "unknown":
                                Debug.WriteLine($"{component.name} version is weird! local: {localVersionString} | online: {onlineVersionString}");
                                needsUpdate = localVersionString != onlineVersionString;
                                break;
                            default:
                                int localNumber = GetVersionNumber(localVersionString);
                                int onlineNumber = GetVersionNumber(onlineVersionString);

                                needsUpdate = localNumber < onlineNumber;
                                break;
                        }
                    }

                    Debug.WriteLine($"{component.name} - local: {localVersionString} | online: {onlineVersionString} | needs update? {needsUpdate}");
                }
                else
                {
                    Debug.WriteLine($"release is null? component: {component.name}");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// When the window is loaded, the update checker is run and DiscordRPC is set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //CHECK IF I LEFT DEBUG SET WRONG!!
#if DEBUG
            //checkForUpdates(false);
#elif !DEBUG
#endif

            if (Lazydata.ParrotData.UseDiscordRPC)
                DiscordRPC.UpdatePresence(new DiscordRPC.RichPresence
                {
                    details = "Main Menu",
                    largeImageKey = "teknoparrot",
                });
        }

        /// <summary>
        /// Loads the AddGame screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddGame(object sender, RoutedEventArgs e)
        {
            contentControl.Content = _addGame;
        }

        private void ColorZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }

            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        
        private void BtnDwPaTool(object sender, RoutedEventArgs e)
        {
            contentControl.Content = _library;
            Process.Start("https://mega.nz/folder/3H4AwbYC#p2IbY4Udgx44PvK1bDTcsw");
        }
    }
}