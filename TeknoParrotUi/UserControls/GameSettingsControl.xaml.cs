using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TeknoParrotUi.Common;
using TeknoParrotUi.Helpers;
using TeknoParrotUi.Views;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Data;
using System.Collections;

namespace TeknoParrotUi.UserControls
{
    /// <summary>
    /// Interaction logic for GameSettingsControl.xaml
    /// </summary>
    public partial class GameSettingsControl : UserControl
    {
        public GameSettingsControl()
        {
            InitializeComponent();
        }

        private GameProfile _gameProfile;
        private ListBoxItem _comboItem;
        private ContentControl _contentControl;
        private Library _library;
        private InputApi _inputApi = InputApi.DirectInput;

        public void LoadNewSettings(GameProfile gameProfile, ListBoxItem comboItem, ContentControl contentControl, Library library)
        {
            _gameProfile = gameProfile;
            _comboItem = comboItem;

            GamePathBox.Text = _gameProfile.GamePath;
            GamePathBox2.Text = _gameProfile.GamePath2;

            GameSettingsList.ItemsSource = gameProfile.ConfigValues;
            _contentControl = contentControl;
            _library = library;

            string exeName = "";

            if (!string.IsNullOrEmpty(_gameProfile.ExecutableName))
                exeName = $" ({_gameProfile.ExecutableName})".Replace(";", " or ");

            GameExecutableText.Text = $"Game Executable{exeName}:";

            if (_gameProfile.HasTwoExecutables)
            {
                exeName = "";

                if (!string.IsNullOrEmpty(_gameProfile.ExecutableName2))
                    exeName = $" ({_gameProfile.ExecutableName2})".Replace(";", " or ");

                GameExecutable2Text.Text = $"Second Game Executable{exeName}:";

                GameExecutable2Text.Visibility = Visibility.Visible;
                GamePathBox2.Visibility = Visibility.Visible;
            }
            else
            {
                GameExecutable2Text.Visibility = Visibility.Collapsed;
                GamePathBox2.Visibility = Visibility.Collapsed;
            }

            // Clear All Previous IP to prevent duplicate print
            _gameProfile.ConfigValues.Find(cv => cv.FieldName == "NetworkAdapterIP").FieldOptions.Clear();

            // Get Softether VPN IP for VS stuff
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface Interface in interfaces)
                {
                    if (Interface.Description.Contains("VPN Client Adapter - VPN")
                        && Interface.OperationalStatus == OperationalStatus.Up)
                    {
                        var vpn = NetworkInterface.GetAllNetworkInterfaces().First(x => x.Name == "VPN - VPN Client");
                        var ips = vpn.GetIPProperties().UnicastAddresses.First(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Address.ToString();
                        bool nullornot = string.IsNullOrWhiteSpace(ips);
                        if (nullornot == false)
                        {
                            _gameProfile.ConfigValues.Find(cv => cv.FieldName == "NetworkAdapterIP").FieldOptions.Add(ips);
                            
                        }
                    }
                }
            }

            // Get Local Network IP
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            _gameProfile.ConfigValues.Find(cv => cv.FieldName == "NetworkAdapterIP").FieldOptions.Add(ip.Address.ToString());
                        }
                    }
                }
            }
        }

        private void SelectExecutableForTextBox(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                CheckFileExists = true,
                Title = Properties.Resources.GameSettingsSelectGameExecutable
            };

            if (!string.IsNullOrEmpty(_gameProfile.ExecutableName))
            {
                openFileDialog.Filter = $"{Properties.Resources.GameSettingsGameExecutableFilter} ({_gameProfile.ExecutableName})|{_gameProfile.ExecutableName}|All files (*.*)|*.*";
            }

            if (openFileDialog.ShowDialog() == true)
            {
                ((TextBox)sender).Text = openFileDialog.FileName;
            }
        }

        private void SelectExecutable2ForTextBox(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                CheckFileExists = true,
                Title = Properties.Resources.GameSettingsSelectGameExecutable
            };

            if (!string.IsNullOrEmpty(_gameProfile.ExecutableName2))
            {
                openFileDialog.Filter = $"{Properties.Resources.GameSettingsGameExecutableFilter} ({_gameProfile.ExecutableName2})|{_gameProfile.ExecutableName2}|All files (*.*)|*.*";
            }

            if (openFileDialog.ShowDialog() == true)
            {
                ((TextBox)sender).Text = openFileDialog.FileName;
            }
        }

        private void BtnSaveSettings(object sender, RoutedEventArgs e)
        {
            string inputApiString = _gameProfile.ConfigValues.Find(cv => cv.FieldName == "Input API")?.FieldValue;

            if (inputApiString != null)
                _inputApi = (InputApi)Enum.Parse(typeof(InputApi), inputApiString);

            foreach (var t in _gameProfile.JoystickButtons)
            {
                if (_inputApi == InputApi.DirectInput)
                    t.BindName = t.BindNameDi;
                else if (_inputApi == InputApi.XInput)
                    t.BindName = t.BindNameXi;
                else if (_inputApi == InputApi.RawInput)
                    t.BindName = t.BindNameRi;
            }

            string BorderlessValue = _gameProfile.ConfigValues.Find(cv => cv.FieldName == "Borderless Fullscreen")?.FieldValue;
            if (BorderlessValue == "1")
            {
                _gameProfile.ConfigValues.Find(cv => cv.FieldName == "Windowed").FieldValue = "0";
            }

            if (_gameProfile.EmulationProfile == EmulationProfile.NamcoWmmt5)
            {
                JoystickHelper.SerializeGameProfile(_gameProfile);
                _gameProfile.GamePath = GamePathBox.Text;
                _gameProfile.GamePath2 = GamePathBox2.Text;
                JoystickHelper.SerializeGameProfile(_gameProfile);
                _comboItem.Tag = _gameProfile;
                Application.Current.Windows.OfType<MainWindow>().Single().ShowMessage(string.Format(Properties.Resources.SuccessfullySaved, System.IO.Path.GetFileName(_gameProfile.FileName)));
                _library.ListUpdate(_gameProfile.GameName);
                _contentControl.Content = _library;
            }
        }

        private void BtnGoBack(object sender, RoutedEventArgs e)
        {
            // Reload library to discard changes
            _library.ListUpdate(_gameProfile.GameName);

            _contentControl.Content = _library;
        }
    }
}
