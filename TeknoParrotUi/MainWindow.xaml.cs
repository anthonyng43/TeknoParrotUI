using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
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

            bool hasRedist = true;
            bool hasDirectX = true;

            string redistFile = "msvcp100.dll";
            string directXFile = "D3DCompiler_43.dll";
            
            if (Environment.Is64BitOperatingSystem)
            {
                if (!File.Exists(Path.Combine(sysWOW64Dir, redistFile)))
                {
                    hasRedist = false;
                }

                if (!File.Exists(Path.Combine(sysWOW64Dir, directXFile)))
                {
                    hasDirectX = false;
                }
            }
            else
            {
                if (!File.Exists(Path.Combine(systemDir, redistFile)))
                {
                    hasRedist = false;
                }

                if (!File.Exists(Path.Combine(systemDir, redistFile)))
                {
                    hasDirectX = false;
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
                    string commands = "";
                    if (!hasRedist)
                    {
                        commands = @"
                        @echo off
                        cd /d ""%TEMP%""
                        echo Downloading Visual C++ AIO...
                        powershell -c ""(New-Object System.Net.WebClient).DownloadFile('https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe','vcredist_aio.exe')""
                        echo Installing Visual C++ Redistributables...
                        vcredist_aio.exe /y
                        del vcredist_aio.exe /Q
                        echo Done.
                        echo Please relaunch TeknoParrotUi manually if it doesn't launch & pause
                        ";
                    }
                    else if (!hasDirectX)
                    {
                        commands = @"
                        @echo off
                        cd /d ""%TEMP%""
                        echo Downloading DirectX...
                        powershell -c ""(New-Object System.Net.WebClient).DownloadFile('https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe','dxwebsetup.exe')""
                       echo Installing DirectX...
                        dxwebsetup.exe /Q
                        echo Delete downloaded files...
                        del dxwebsetup.exe /Q
                        echo Done.
                        echo Please relaunch TeknoParrotUi manually if it doesn't launch & pause
                        ";
                    }
                    else
                    {
                        commands = @"
                        @echo off
                        cd /d ""%TEMP%""
                        echo Downloading DirectX...
                        powershell -c ""(New-Object System.Net.WebClient).DownloadFile('https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe','dxwebsetup.exe')""
                        echo Downloading Visual C++ AIO...
                        powershell -c ""(New-Object System.Net.WebClient).DownloadFile('https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe','vcredist_aio.exe')""
                        echo Installing DirectX...
                        dxwebsetup.exe /Q
                        echo Installing Visual C++ Redistributables...
                        vcredist_aio.exe /y
                        echo Delete downloaded files...
                        del dxwebsetup.exe /Q
                        del vcredist_aio.exe /Q
                        echo Done.
                        echo Please relaunch TeknoParrotUi manually if it doesn't launch & pause
                        ";
                    }

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