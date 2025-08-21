using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using SharpDX.DirectInput;
using TeknoParrotUi.Common.InputProfiles.Helpers;
using TeknoParrotUi.Common.Jvs;
using DeviceType = SharpDX.DirectInput.DeviceType;

namespace TeknoParrotUi.Common.InputListening
{
    public class InputListenerDirectInput
    {
        private static GameProfile _gameProfile;
        public static bool KillMe;
        public static bool DisableTestButton;
        private static readonly DirectInput _diInput = new DirectInput();
        private static bool mkdxTest = false;
        private static bool changeWmmt5GearUp = false;
        private static bool changeWmmt5GearDown = false;
        private static bool changeSrcGearUp = false;
        private static bool changeSrcGearDown = false;
        private static bool KeyboardGasDown = false;
        private static bool KeyboardBrakeDown = false;
        private static bool KeyboardWheelLeft = false;
        private static bool KeyboardWheelRight = false;
        private static bool KeyboardAnalogLeft = false;
        private static bool KeyboardAnalogRight = false;
        private static bool KeyboardAnalogReverseDown = false;
        private static bool KeyboardAnalogReverseUp = false;
        private static bool KeyboardSWThrottleDown = false;
        private static bool KeyboardSWThrottleUp = false;
        private static bool KeyboardHandlebarLeft = false;
        private static bool KeyboardHandlebarRight = false;
        private static bool KeyboardorButtonAxis = false;
        private static bool ReverseSWThrottleAxis = false;
        private static bool KeyboardWheelActivate = false;
        private static bool KeyboardGasActivate = false;
        private static bool KeyboardBrakeActivate = false;
        private static bool KeyboardAnalogXActivate = false;
        private static bool KeyboardAnalogYActivate = false;
        private static bool KeyboardSWThrottleActivate = false;
        private static bool KeyboardHandlebarActivate = false;
        private static bool RelativeInput = false;
        private static bool RelativeTimer = false;
        private static bool KeyboardForAxisTimer = false;
        private static System.Timers.Timer timer = new System.Timers.Timer(16);
        private static System.Timers.Timer Relativetimer = new System.Timers.Timer(32);
        private static int minValWheel;
        private static int cntVal;
        private static int maxValWheel;
        private static int maxGasBrake;
        private static int minGasBrake;
        private static int KeyboardWheelValue;
        private static int KeyboardGasValue;
        private static int KeyboardBrakeValue;
        private static int KeyboardThrottleValue;
        private static int KeyboardHandlebarValue;
        private static int KeyboardAnalogAxisSensitivity;
        private static int KeyboardAcclBrakeAxisSensitivity;
        private static int KeyboardHandlebarAxisSensitivity;
        private static int WheelAnalogByteValue = -1;
        private static int GasAnalogByteValue = -1;
        private static int BrakeAnalogByteValue = -1;
        private static int ThrottleAnalogByteValue = -1;
        private static int HandlebarAnalogByteValue = -1;

        /// <summary>
        /// Checks if joystick or gamepad GUID is found.
        /// </summary>
        /// <param name="joystickGuid">Joystick GUID;:</param>
        /// <returns></returns>
        private bool DoesJoystickExist(Guid joystickGuid)
        {
            if (File.Exists("DirectInputOverride.txt"))
            {
                // Don't care about filters when using override!
                return _diInput.GetDevices().Any(x => x.InstanceGuid == joystickGuid);
            }

            return _diInput.GetDevices()
                .Any(
                    x => x.InstanceGuid == joystickGuid && x.Type != DeviceType.Device);
        }

        public void ListenDirectInput(List<JoystickButtons> joystickButtons, GameProfile gameProfile)
        {
            _gameProfile = gameProfile;
            var guids = new List<Guid>();
            changeWmmt5GearUp = false;
            changeWmmt5GearDown = false;
            changeSrcGearDown = false;
            changeSrcGearUp = false;
            mkdxTest = false;

            KeyboardorButtonAxis = gameProfile.ConfigValues.Any(x => x.FieldName == "Use Keyboard/Button For Axis" && x.FieldValue == "1");
            ReverseSWThrottleAxis = gameProfile.ConfigValues.Any(x => x.FieldName == "Reverse Throttle Axis" && x.FieldValue == "1");

            minValWheel = 0x00;
            maxValWheel = 0xFF;
            cntVal = 0x80;
            minGasBrake = 0x00;
            maxGasBrake = 0xFF;
            
            if (_gameProfile.EmulationProfile == EmulationProfile.NamcoWmmt5)
            {
                InputCode.AnalogBytes[0] = 0x80;
                WheelAnalogByteValue = 0;
                GasAnalogByteValue = 2;
                BrakeAnalogByteValue = 4;
            }

            if (KeyboardorButtonAxis)
            {
                if (_gameProfile.EmulationProfile == EmulationProfile.NamcoWmmt5)
                {
                    var KeyboardAnalogAxisSensitivityA = gameProfile.ConfigValues.FirstOrDefault(x => x.FieldName == "Keyboard/Button Axis Wheel Sensitivity" || x.FieldNameJp == "キーボード/ボタン軸ホイール感度");
                    if (KeyboardAnalogAxisSensitivityA != null)
                    {
                        KeyboardAnalogAxisSensitivity = System.Convert.ToInt32(KeyboardAnalogAxisSensitivityA.FieldValue);
                    }

                    var KeyboardAcclBrakeAxisSensitivityA = gameProfile.ConfigValues.FirstOrDefault(x => x.FieldName == "Keyboard/Button Axis Pedal Sensitivity" || x.FieldNameJp == "キーボード/ボタン軸ホイール感度");
                    if (KeyboardAcclBrakeAxisSensitivityA != null)
                    {
                        KeyboardAcclBrakeAxisSensitivity = System.Convert.ToInt32(KeyboardAcclBrakeAxisSensitivityA.FieldValue);
                    }
                }

                if (GasAnalogByteValue >= 0)
                {
                    KeyboardGasValue = InputCode.AnalogBytes[GasAnalogByteValue];
                }
                if (BrakeAnalogByteValue >= 0)
                {
                    KeyboardBrakeValue = InputCode.AnalogBytes[BrakeAnalogByteValue];
                }
                if (ThrottleAnalogByteValue >= 0)
                {
                    KeyboardThrottleValue = InputCode.AnalogBytes[ThrottleAnalogByteValue];
                }
                if (HandlebarAnalogByteValue >= 0)
                {
                    KeyboardHandlebarValue = InputCode.AnalogBytes[HandlebarAnalogByteValue];
                }
                if (WheelAnalogByteValue >= 0)
                {
                    KeyboardWheelValue = InputCode.AnalogBytes[WheelAnalogByteValue];
                }
                if (!KeyboardForAxisTimer)
                {
                    KeyboardForAxisTimer = true;
                    timer.Elapsed += ListenKeyboardButton;
                }
                timer.Start();
            }

            // Find individual guis so we can listen.

            var nonNullButtons = joystickButtons.Where(x => x?.DirectInputButton != null).ToList();

            foreach (var t in nonNullButtons)
            {
                if (guids.All(x => x != t.DirectInputButton?.JoystickGuid))
                {
                    guids.Add(t.DirectInputButton.JoystickGuid);
                }
            }

            // Remove guids that we cannot listen!
            for (int i = 0; i < guids.Count; i++)
            {
                if (!DoesJoystickExist(guids[i]))
                {
                    guids.RemoveAt(i);
                    i = 0;
                }
            }

            // Spawn listeners
            foreach (var guid in guids)
            {
                var joystick = new Joystick(_diInput, guid);
                // Set BufferSize in order to use buffered data.
                joystick.Properties.BufferSize = 512;
                joystick.Acquire();
                var thread = new Thread(() => ListenJoystick(nonNullButtons.Where(x => x.DirectInputButton.JoystickGuid == guid).ToList(), joystick));
                thread.Start();
            }

            while (!KillMe)
                Thread.Sleep(5000);
        }

        private void ListenKeyboardButton(object sender, ElapsedEventArgs e)
        {
            if ((WheelAnalogByteValue >= 0) && (KeyboardWheelActivate))
            {
                if ((KeyboardWheelRight) && (KeyboardWheelLeft))
                {
                    InputCode.AnalogBytes[WheelAnalogByteValue] = (byte)KeyboardWheelValue;
                }
                else if (KeyboardWheelRight)
                {
                    InputCode.AnalogBytes[WheelAnalogByteValue] = (byte)Math.Min(maxValWheel, KeyboardWheelValue + KeyboardAnalogAxisSensitivity);
                }
                else if (KeyboardWheelLeft)
                {
                    InputCode.AnalogBytes[WheelAnalogByteValue] = (byte)Math.Max(minValWheel, KeyboardWheelValue - KeyboardAnalogAxisSensitivity);
                }
                else
                {
                    if (KeyboardWheelValue < cntVal)
                    {
                        InputCode.AnalogBytes[WheelAnalogByteValue] = (byte)Math.Min(cntVal, KeyboardWheelValue + KeyboardAnalogAxisSensitivity);
                    }
                    else if (KeyboardWheelValue > cntVal)
                    {
                        InputCode.AnalogBytes[WheelAnalogByteValue] = (byte)Math.Max(cntVal, KeyboardWheelValue - KeyboardAnalogAxisSensitivity);
                    }
                    else
                    {
                        InputCode.AnalogBytes[WheelAnalogByteValue] = (byte)cntVal;
                    }
                }

                KeyboardWheelValue = InputCode.AnalogBytes[WheelAnalogByteValue];
            }

            if ((BrakeAnalogByteValue >= 0) && (KeyboardBrakeActivate))
            {
                if (KeyboardBrakeDown)
                {
                    InputCode.AnalogBytes[BrakeAnalogByteValue] = (byte)Math.Min(maxGasBrake, KeyboardBrakeValue + KeyboardAcclBrakeAxisSensitivity);
                }
                else
                {
                    InputCode.AnalogBytes[BrakeAnalogByteValue] = (byte)Math.Max(minGasBrake, KeyboardBrakeValue - KeyboardAcclBrakeAxisSensitivity);
                }
                KeyboardBrakeValue = InputCode.AnalogBytes[BrakeAnalogByteValue];
            }

            if ((GasAnalogByteValue >= 0) && (KeyboardGasActivate))
            {
                if (KeyboardGasDown)
                {
                    InputCode.AnalogBytes[GasAnalogByteValue] = (byte)Math.Min(maxGasBrake, KeyboardGasValue + KeyboardAcclBrakeAxisSensitivity);
                }
                else
                {
                    InputCode.AnalogBytes[GasAnalogByteValue] = (byte)Math.Max(minGasBrake, KeyboardGasValue - KeyboardAcclBrakeAxisSensitivity);
                }
                KeyboardGasValue = InputCode.AnalogBytes[GasAnalogByteValue];
            }

            if ((ThrottleAnalogByteValue >= 0) && (KeyboardSWThrottleActivate))
            {
                if ((KeyboardSWThrottleDown) && (KeyboardSWThrottleUp))
                {
                    InputCode.AnalogBytes[ThrottleAnalogByteValue] = (byte)KeyboardThrottleValue;
                }
                else if (KeyboardSWThrottleDown)
                {
                    InputCode.AnalogBytes[ThrottleAnalogByteValue] = (byte)Math.Max(0x00, KeyboardThrottleValue - KeyboardAcclBrakeAxisSensitivity);
                }
                else if (KeyboardSWThrottleUp)
                {
                    InputCode.AnalogBytes[ThrottleAnalogByteValue] = (byte)Math.Min(0xFF, KeyboardThrottleValue + KeyboardAcclBrakeAxisSensitivity);
                }
                else
                {
                    if (KeyboardThrottleValue < cntVal)
                    {
                        InputCode.AnalogBytes[ThrottleAnalogByteValue] = (byte)Math.Min(cntVal, KeyboardThrottleValue + KeyboardAcclBrakeAxisSensitivity);
                    }
                    else if (KeyboardThrottleValue > cntVal)
                    {
                        InputCode.AnalogBytes[ThrottleAnalogByteValue] = (byte)Math.Min(cntVal, KeyboardThrottleValue + KeyboardAcclBrakeAxisSensitivity);
                    }
                    else
                    {
                        InputCode.AnalogBytes[ThrottleAnalogByteValue] = (byte)cntVal;
                    }
                }
                KeyboardThrottleValue = InputCode.AnalogBytes[ThrottleAnalogByteValue];
            }

            if ((HandlebarAnalogByteValue >= 0) && (KeyboardHandlebarActivate))
            {
                if ((KeyboardHandlebarRight) && (KeyboardHandlebarLeft))
                {
                    InputCode.AnalogBytes[HandlebarAnalogByteValue] = (byte)KeyboardHandlebarValue;
                }
                else if (KeyboardHandlebarRight)
                {
                    InputCode.AnalogBytes[HandlebarAnalogByteValue] = (byte)Math.Min(0xFF, KeyboardHandlebarValue + KeyboardHandlebarAxisSensitivity);
                }
                else if (KeyboardHandlebarLeft)
                {
                    InputCode.AnalogBytes[HandlebarAnalogByteValue] = (byte)Math.Max(0x00, KeyboardHandlebarValue - KeyboardHandlebarAxisSensitivity);
                }
                else
                {
                    if (KeyboardHandlebarValue < cntVal)
                    {
                        InputCode.AnalogBytes[HandlebarAnalogByteValue] = (byte)Math.Min(cntVal, KeyboardHandlebarValue + KeyboardHandlebarAxisSensitivity);
                    }
                    else if (KeyboardHandlebarValue > cntVal)
                    {
                        InputCode.AnalogBytes[HandlebarAnalogByteValue] = (byte)Math.Max(cntVal, KeyboardHandlebarValue - KeyboardHandlebarAxisSensitivity);
                    }
                    else
                    {
                        InputCode.AnalogBytes[HandlebarAnalogByteValue] = (byte)cntVal;
                    }
                }
                KeyboardHandlebarValue = InputCode.AnalogBytes[HandlebarAnalogByteValue];
            }

            if (KillMe)
            {
                ReverseSWThrottleAxis = false;
                KeyboardWheelActivate = false;
                KeyboardGasActivate = false;
                KeyboardBrakeActivate = false;
                KeyboardAnalogXActivate = false;
                KeyboardAnalogYActivate = false;
                KeyboardSWThrottleActivate = false;
                KeyboardHandlebarActivate = false;
                timer.Enabled = false;
            }
        }      
        private void ListenJoystick(List<JoystickButtons> joystickButtons, Joystick joystick)
        {
            // Poll events from joystick
            try
            { 
                Lazydata.Joystick = joystick;
                while (!KillMe)
                {             
                    joystick.Poll();
                    foreach (var state in joystick.GetBufferedData())
                    {
                        foreach (var t in joystickButtons)
                        {
                            HandleDirectInput(t, state);
                        }
                    }
                    Thread.Sleep(10);
                }
                joystick.Unacquire();
            }
            catch (Exception)
            {
                // ignored
                joystick.Unacquire();
            }
        }

        private void HandleDirectInput(JoystickButtons joystickButtons, JoystickUpdate state)
        {
            var button = joystickButtons.DirectInputButton;
            switch (joystickButtons.InputMapping)
            {
                case InputMapping.Test:
                {
                    if (InputCode.ButtonMode == EmulationProfile.NamcoWmmt5)
                    {
                        var result = DigitalHelper.GetButtonPressDirectInput(button, state);
                        if (result != null && result.Value)
                        {
                            if (mkdxTest)
                            {
                                InputCode.PlayerDigitalButtons[0].Test = false;
                                mkdxTest = false;
                            }
                            else
                            {
                                InputCode.PlayerDigitalButtons[0].Test = true;
                                mkdxTest = true;
                            }
                        }
                    }
                    else
                    {
                        InputCode.PlayerDigitalButtons[0].Test = DigitalHelper.GetButtonPressDirectInput(button, state);
                    }
                    break;
                }
                case InputMapping.Service1:
                    InputCode.PlayerDigitalButtons[0].Service = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.Service2:
                    InputCode.PlayerDigitalButtons[1].Service = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.Coin1:
                    InputCode.PlayerDigitalButtons[0].Coin = DigitalHelper.GetButtonPressDirectInput(button, state);
                    JvsPackageEmulator.UpdateCoinCount(0);
                    break;
                case InputMapping.Coin2:
                    InputCode.PlayerDigitalButtons[1].Coin = DigitalHelper.GetButtonPressDirectInput(button, state);
                    JvsPackageEmulator.UpdateCoinCount(1);
                    break;
                case InputMapping.P1Button1:
                    InputCode.PlayerDigitalButtons[0].Button1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P1Button2:
                    InputCode.PlayerDigitalButtons[0].Button2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P1Button3:
                    InputCode.PlayerDigitalButtons[0].Button3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P1Button4:
                    InputCode.PlayerDigitalButtons[0].Button4 = DigitalHelper.GetButtonPressDirectInput(button, state);

                    break;
                case InputMapping.P1Button5:
                    InputCode.PlayerDigitalButtons[0].Button5 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P1Button6:
                    InputCode.PlayerDigitalButtons[0].Button6 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P1ButtonUp:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.Up);
                    break;
                case InputMapping.P1ButtonDown:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.Down);
                    break;
                case InputMapping.P1ButtonLeft:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.Left);
                    break;
                case InputMapping.P1ButtonRight:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.Right);
                    break;
                case InputMapping.P1RelativeUp:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.RelativeUp);
                    break;
                case InputMapping.P1RelativeDown:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.RelativeDown);
                    break;
                case InputMapping.P1RelativeLeft:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.RelativeLeft);
                    break;
                case InputMapping.P1RelativeRight:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[0], button, state, Direction.RelativeRight);
                    break;
                case InputMapping.P1ButtonStart:
                    InputCode.PlayerDigitalButtons[0].Start = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2Button1:
                    InputCode.PlayerDigitalButtons[1].Button1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2Button2:
                    InputCode.PlayerDigitalButtons[1].Button2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2Button3:
                    InputCode.PlayerDigitalButtons[1].Button3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2Button4:
                    InputCode.PlayerDigitalButtons[1].Button4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2Button5:
                    InputCode.PlayerDigitalButtons[1].Button5 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2Button6:
                    InputCode.PlayerDigitalButtons[1].Button6 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P2ButtonUp:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.Up);
                    break;
                case InputMapping.P2ButtonDown:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.Down);
                    break;
                case InputMapping.P2ButtonLeft:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.Left);
                    break;
                case InputMapping.P2ButtonRight:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.Right);
                    break;
                case InputMapping.P2RelativeUp:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.RelativeUp);
                    break;
                case InputMapping.P2RelativeDown:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.RelativeDown);
                    break;
                case InputMapping.P2RelativeLeft:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.RelativeLeft);
                    break;
                case InputMapping.P2RelativeRight:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[1], button, state, Direction.RelativeRight);
                    break;
                case InputMapping.P2ButtonStart:
                    InputCode.PlayerDigitalButtons[1].Start = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.Analog0:
                    InputCode.SetAnalogByte(0, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog1:
                    InputCode.SetAnalogByte(1, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog2:
                    InputCode.SetAnalogByte(2, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog3:
                    InputCode.SetAnalogByte(3, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog4:
                    InputCode.SetAnalogByte(4, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog5:
                    InputCode.SetAnalogByte(5, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog6:
                    InputCode.SetAnalogByte(6, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog7:
                    InputCode.SetAnalogByte(7, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog8:
                    InputCode.SetAnalogByte(8, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog9:
                    InputCode.SetAnalogByte(9, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog10:
                    InputCode.SetAnalogByte(10, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog11:
                    InputCode.SetAnalogByte(11, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog12:
                    InputCode.SetAnalogByte(12, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog13:
                    InputCode.SetAnalogByte(13, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog14:
                    InputCode.SetAnalogByte(14, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog15:
                    InputCode.SetAnalogByte(15, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog16:
                    InputCode.SetAnalogByte(16, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog17:
                    InputCode.SetAnalogByte(17, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog18:
                    InputCode.SetAnalogByte(18, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog19:
                    InputCode.SetAnalogByte(19, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog20:
                    InputCode.SetAnalogByte(20, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.SrcGearChange1:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        DigitalHelper.ChangeSrcGear(1);
                    }
                }
                    break;
                case InputMapping.SrcGearChange2:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        DigitalHelper.ChangeSrcGear(2);
                    }
                }
                    break;
                case InputMapping.SrcGearChange3:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        DigitalHelper.ChangeSrcGear(3);
                    }
                }
                    break;
                case InputMapping.SrcGearChange4:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        DigitalHelper.ChangeSrcGear(4);
                    }
                }
                    break;
                case InputMapping.ExtensionOne1:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne2:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton2 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne3:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton3 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne4:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton4 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne11:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_1 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne12:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_2 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne13:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_3 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne14:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_4 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne15:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_5 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne16:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_6 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne17:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_7 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionOne18:
                    InputCode.PlayerDigitalButtons[0].ExtensionButton1_8 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo1:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo2:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton2 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo3:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton3 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo4:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton4 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo11:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_1 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo12:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_2 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo13:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_3 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo14:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_4 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo15:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_5 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo16:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_6 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo17:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_7 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.ExtensionTwo18:
                    InputCode.PlayerDigitalButtons[1].ExtensionButton1_8 =
                        DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.Analog0Special1:
                case InputMapping.Analog0Special2:
                    InputCode.SetAnalogByte(0, ModifyAnalog(joystickButtons, state));
                    break;
                case InputMapping.Analog2Special1:
                case InputMapping.Analog2Special2:
                    InputCode.SetAnalogByte(2, ModifyAnalog(joystickButtons, state));
                    break;

                // Jvs Board 2

                case InputMapping.JvsTwoService1:
                    InputCode.PlayerDigitalButtons[2].Service = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoService2:
                    InputCode.PlayerDigitalButtons[3].Service = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoCoin1:
                    InputCode.PlayerDigitalButtons[2].Coin = DigitalHelper.GetButtonPressDirectInput(button, state);
                    JvsPackageEmulator.UpdateCoinCount(2);
                    break;
                case InputMapping.JvsTwoCoin2:
                    InputCode.PlayerDigitalButtons[3].Coin = DigitalHelper.GetButtonPressDirectInput(button, state);
                    JvsPackageEmulator.UpdateCoinCount(3);
                    break;
                case InputMapping.JvsTwoP1Button1:
                    InputCode.PlayerDigitalButtons[2].Button1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP1Button2:
                    InputCode.PlayerDigitalButtons[2].Button2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP1Button3:
                    InputCode.PlayerDigitalButtons[2].Button3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP1Button4:
                    InputCode.PlayerDigitalButtons[2].Button4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP1Button5:
                    InputCode.PlayerDigitalButtons[2].Button5 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP1Button6:
                    InputCode.PlayerDigitalButtons[2].Button6 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP1ButtonUp:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.Up);
                    break;
                case InputMapping.JvsTwoP1ButtonDown:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.Down);
                    break;
                case InputMapping.JvsTwoP1ButtonLeft:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.Left);
                    break;
                case InputMapping.JvsTwoP1ButtonRight:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.Right);
                    break;
                case InputMapping.JvsTwoP1ButtonStart:
                    InputCode.PlayerDigitalButtons[2].Start = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2Button1:
                    InputCode.PlayerDigitalButtons[3].Button1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2Button2:
                    InputCode.PlayerDigitalButtons[3].Button2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2Button3:
                    InputCode.PlayerDigitalButtons[3].Button3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2Button4:
                    InputCode.PlayerDigitalButtons[3].Button4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2Button5:
                    InputCode.PlayerDigitalButtons[3].Button5 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2Button6:
                    InputCode.PlayerDigitalButtons[3].Button6 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoP2ButtonUp:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.Up);
                    break;
                case InputMapping.JvsTwoP2ButtonDown:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.Down);
                    break;
                case InputMapping.JvsTwoP2ButtonLeft:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.Left);
                    break;
                case InputMapping.JvsTwoP2ButtonRight:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.Right);
                    break;
                case InputMapping.JvsTwoP2ButtonStart:
                    InputCode.PlayerDigitalButtons[3].Start = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;

                case InputMapping.JvsTwoExtensionOne1:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne2:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne3:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne4:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne11:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne12:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne13:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne14:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne15:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_5 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne16:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_6 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne17:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_7 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionOne18:
                    InputCode.PlayerDigitalButtons[2].ExtensionButton1_8 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo1:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo2:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo3:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo4:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo11:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_1 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo12:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_2 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo13:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_3 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo14:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_4 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo15:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_5 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo16:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_6 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo17:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_7 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.JvsTwoExtensionTwo18:
                    InputCode.PlayerDigitalButtons[3].ExtensionButton1_8 = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;

                case InputMapping.JvsTwoAnalog0:
                    InputCode.SetAnalogByte(0, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog1:
                    InputCode.SetAnalogByte(1, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog2:
                    InputCode.SetAnalogByte(2, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog3:
                    InputCode.SetAnalogByte(3, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog4:
                    InputCode.SetAnalogByte(4, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog5:
                    InputCode.SetAnalogByte(5, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog6:
                    InputCode.SetAnalogByte(6, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog7:
                    InputCode.SetAnalogByte(7, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog8:
                    InputCode.SetAnalogByte(8, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog9:
                    InputCode.SetAnalogByte(9, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog10:
                    InputCode.SetAnalogByte(10, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog11:
                    InputCode.SetAnalogByte(11, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog12:
                    InputCode.SetAnalogByte(12, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog13:
                    InputCode.SetAnalogByte(13, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog14:
                    InputCode.SetAnalogByte(14, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog15:
                    InputCode.SetAnalogByte(15, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog16:
                    InputCode.SetAnalogByte(16, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog17:
                    InputCode.SetAnalogByte(17, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog18:
                    InputCode.SetAnalogByte(18, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog19:
                    InputCode.SetAnalogByte(19, ModifyAnalog(joystickButtons, state), true);
                    break;
                case InputMapping.JvsTwoAnalog20:
                    InputCode.SetAnalogByte(20, ModifyAnalog(joystickButtons, state), true);
                    break;

                case InputMapping.Wmmt5GearChange1:
                {
                    var pressed = DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state);
                    if (pressed != null)
                    {
                        DigitalHelper.ChangeWmmt5Gear((bool)pressed ? 1 : 0);
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChange2:
                {
                    var pressed = DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state);
                    if (pressed != null)
                    {
                        DigitalHelper.ChangeWmmt5Gear((bool)pressed ? 2 : 0);
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChange3:
                {
                    var pressed = DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state);
                    if (pressed != null)
                    {
                        DigitalHelper.ChangeWmmt5Gear((bool)pressed ? 3 : 0);
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChange4:
                {
                    var pressed = DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state);
                    if (pressed != null)
                    {
                        DigitalHelper.ChangeWmmt5Gear((bool)pressed ? 4 : 0);
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChange5:
                {
                    var pressed = DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state);
                    if (pressed != null)
                    {
                        DigitalHelper.ChangeWmmt5Gear((bool)pressed ? 5 : 0);
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChange6:
                {
                    var pressed = DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state);
                    if (pressed != null)
                    {
                        DigitalHelper.ChangeWmmt5Gear((bool)pressed ? 6 : 0);
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChangeUp:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        if(!changeWmmt5GearUp)
                            DigitalHelper.ChangeWmmt5GearUp();
                        changeWmmt5GearUp = true;
                    }
                    else
                    {
                        changeWmmt5GearUp = false;
                    }
                }
                    break;
                case InputMapping.Wmmt5GearChangeDown:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        if(!changeWmmt5GearDown)
                            DigitalHelper.ChangeWmmt5GearDown();
                        changeWmmt5GearDown = true;
                    }
                    else
                    {
                        changeWmmt5GearDown = false;
                    }
                }
                    break;
                case InputMapping.SrcGearChangeUp:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        if (!changeSrcGearUp)
                            DigitalHelper.ChangeSrcGearUp();
                        changeSrcGearUp = true;
                    }
                    else
                    {
                        changeSrcGearUp = false;
                    }
                }
                    break;
                case InputMapping.SrcGearChangeDown:
                {
                    if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                    {
                        if (!changeSrcGearDown)
                            DigitalHelper.ChangeSrcGearDown();
                        changeSrcGearDown = true;
                    }
                    else
                    {
                        changeSrcGearDown = false;
                    }
                }
                    break;
                case InputMapping.PokkenButtonUp:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PokkenInputButtons, button, state, Direction.Up);
                    break;
                case InputMapping.PokkenButtonDown:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PokkenInputButtons, button, state, Direction.Down);
                    break;
                case InputMapping.PokkenButtonLeft:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PokkenInputButtons, button, state, Direction.Left);
                    break;
                case InputMapping.PokkenButtonRight:
                    DigitalHelper.GetDirectionPressDirectInput(InputCode.PokkenInputButtons, button, state, Direction.Right);
                    break;
                case InputMapping.PokkenButtonStart:
                    InputCode.PokkenInputButtons.Start = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.PokkenButtonA:
                    InputCode.PokkenInputButtons.ButtonA = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.PokkenButtonB:
                    InputCode.PokkenInputButtons.ButtonB = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.PokkenButtonX:
                    InputCode.PokkenInputButtons.ButtonX = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.PokkenButtonY:
                    InputCode.PokkenInputButtons.ButtonY = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.PokkenButtonL:
                    InputCode.PokkenInputButtons.ButtonL = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.PokkenButtonR:
                    InputCode.PokkenInputButtons.ButtonR = DigitalHelper.GetButtonPressDirectInput(button, state);
                    break;
                case InputMapping.P3RelativeUp:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.RelativeUp);
                    break;
                case InputMapping.P3RelativeDown:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.RelativeDown);
                    break;
                case InputMapping.P3RelativeLeft:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.RelativeLeft);
                    break;
                case InputMapping.P3RelativeRight:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[2], button, state, Direction.RelativeRight);
                    break;
                case InputMapping.P4RelativeUp:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.RelativeUp);
                    break;
                case InputMapping.P4RelativeDown:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.RelativeDown);
                    break;
                case InputMapping.P4RelativeLeft:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.RelativeLeft);
                    break;
                case InputMapping.P4RelativeRight:
                    DigitalHelper.GetRelativeDirectionPressDirectInput(InputCode.PlayerDigitalButtons[3], button, state, Direction.RelativeRight);
                    break;
                case InputMapping.Wmmt3InsertCard:
                    {
                        if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                        {
                            WMMT3Cards.InsertCard();
                        }
                    }
                    break;
                case InputMapping.WmmtTapBanapass:
                    {
                        if (DigitalHelper.GetButtonPressDirectInput(joystickButtons.DirectInputButton, state) == true)
                        {
                            WMMTBanapass.TapBanapass();
                        }
                    }
                    break;
                default:
                    break;
                    //throw new ArgumentOutOfRangeException();
            }
        }

        private byte? ModifyAnalog(JoystickButtons joystickButtons, JoystickUpdate state)
        {
            if (joystickButtons.DirectInputButton == null)
                return null;
            if ((JoystickOffset) joystickButtons.DirectInputButton.Button != state.Offset)
                return null;

            switch (joystickButtons.AnalogType)
            {
                case AnalogType.None:
                    break;
                case AnalogType.AnalogJoystick:
                {
                    var analogPos = JvsHelper.CalculateWheelPos(state.Value);

                    if (KeyboardorButtonAxis)
                    {
                        if (joystickButtons.ButtonName.Equals("Joystick Analog X") || joystickButtons.ButtonName.Equals("Analog X"))
                        {
                            break;
                        }
                        if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                        {
                            if (!KeyboardAnalogXActivate)
                            { 
                                KeyboardAnalogXActivate = true;
                            }
                            if (joystickButtons.ButtonName.Contains("Right"))
                            {
                                if (!KeyboardAnalogRight)
                                {
                                    KeyboardAnalogRight = true;
                                }
                                else
                                {
                                    KeyboardAnalogRight = false;
                                }
                            }
                            else
                            {
                                if (!KeyboardAnalogLeft)
                                {
                                    KeyboardAnalogLeft = true;
                                }
                                else
                                {
                                    KeyboardAnalogLeft = false;
                                }
                            }
                            break;
                        }
                        else
                        {
                            if (KeyboardAnalogXActivate)
                            {
                                KeyboardAnalogXActivate = false;
                            }
                        }
                    }
                    else
                    {
                        if (joystickButtons.ButtonName.Equals("Joystick Analog X Left") || joystickButtons.ButtonName.Equals("Joystick Analog X Right") || joystickButtons.ButtonName.Equals("Analog X Left") || joystickButtons.ButtonName.Equals("Analog X Right"))
                        {
                            break;
                        }
                    }

                    return analogPos;
                }
                case AnalogType.AnalogJoystickReverse:
                {
                    byte analogReversePos = 0;
                    analogReversePos = (byte)~JvsHelper.CalculateWheelPos(state.Value);

                    if (KeyboardorButtonAxis)
                    {
                        if (joystickButtons.ButtonName.Equals("Joystick Analog Y") || joystickButtons.ButtonName.Equals("Analog Y"))
                        {
                            break;
                        }
                        if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                        {
                            if (!KeyboardAnalogYActivate)
                            {
                                KeyboardAnalogYActivate = true;
                            }
                            if (joystickButtons.ButtonName.Contains("Down"))
                            {
                                if (!KeyboardAnalogReverseDown)
                                {
                                    KeyboardAnalogReverseDown = true;
                                }
                                else
                                {
                                    KeyboardAnalogReverseDown = false;
                                }
                            }
                            else
                            {
                                if (!KeyboardAnalogReverseUp)
                                {
                                    KeyboardAnalogReverseUp = true;
                                }
                                else
                                {
                                    KeyboardAnalogReverseUp = false;
                                }
                            }
                            break;
                        }
                        else
                        {
                            if (KeyboardAnalogYActivate)
                            {
                                KeyboardAnalogYActivate = false;
                            }
                        }
                    }
                    else
                    {
                        if (joystickButtons.ButtonName.Equals("Joystick Analog Y Up") || joystickButtons.ButtonName.Equals("Joystick Analog Y Down") || joystickButtons.ButtonName.Equals("Analog Y Up") || joystickButtons.ButtonName.Equals("Analog Y Down"))
                        {
                            break;
                        }
                    }
                    return analogReversePos;
                }
                case AnalogType.Gas:
                {
                    var gas = HandleGasBrakeForJvs(state.Value, joystickButtons.DirectInputButton?.IsAxisMinus, Lazydata.ParrotData.ReverseAxisGas, Lazydata.ParrotData.FullAxisGas, true);
                    //Console.WriteLine("Gas: " + gas.ToString("X2"));
                    if (InputCode.ButtonMode == EmulationProfile.NamcoWmmt5)
                    {
                        gas /= 3;
                    }
                        if (KeyboardorButtonAxis)
                        {
                            if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                            {
                                if (!KeyboardGasActivate)
                                {
                                    KeyboardGasActivate = true;
                                }
                                if (!KeyboardGasDown)
                                {
                                    KeyboardGasDown = true;
                                }
                                else
                                {
                                    KeyboardGasDown = false;
                                }
                                break;
                            }
                        }                  
                    return gas;
                }
                case AnalogType.SWThrottle:
                {
                        byte gas = 0;
                        if (ReverseSWThrottleAxis)
                        {
                            gas = HandleGasBrakeForJvs(state.Value, joystickButtons.DirectInputButton?.IsAxisMinus, false, true, false);
                        }
                        else
                        {
                            gas = HandleGasBrakeForJvs(state.Value, joystickButtons.DirectInputButton?.IsAxisMinus, true, true, false);
                        }

                        if (KeyboardorButtonAxis)
                        {
                            if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                            {
                                if (!KeyboardSWThrottleActivate)
                                {
                                    KeyboardSWThrottleActivate = true;
                                }
                                if (joystickButtons.ButtonName.Contains("Brake"))
                                {
                                    if (!KeyboardSWThrottleDown)
                                    {
                                        KeyboardSWThrottleDown = true;
                                    }
                                    else
                                    {
                                        KeyboardSWThrottleDown = false;
                                    }
                                }
                                else
                                {
                                    if (!KeyboardSWThrottleUp)
                                    {
                                        KeyboardSWThrottleUp = true;
                                    }
                                    else
                                    {
                                        KeyboardSWThrottleUp = false;
                                    }
                                }
                                break;
                            }
                            else
                            {
                                if (KeyboardSWThrottleActivate)
                                {
                                    KeyboardSWThrottleActivate = false;
                                }
                            }
                        }
                        else
                        {
                            if (joystickButtons.ButtonName.Equals("Throttle Brake"))
                            {
                                break;
                            }
                        }
                        return gas;
                }
                case AnalogType.SWThrottleReverse:
                {
                        byte gas = 0;
                        if (ReverseSWThrottleAxis)
                        {
                            gas = HandleGasBrakeForJvs(state.Value, joystickButtons.DirectInputButton?.IsAxisMinus, true, true, false);                            
                        }
                        else
                        {
                            gas = HandleGasBrakeForJvs(state.Value, joystickButtons.DirectInputButton?.IsAxisMinus, false, true, false);
                        }
                        if (KeyboardorButtonAxis)
                        {
                            if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                            {
                                if (!KeyboardSWThrottleActivate)
                                {
                                    KeyboardSWThrottleActivate = true;
                                }
                                if (joystickButtons.ButtonName.Contains("Brake"))
                                {
                                    if (!KeyboardSWThrottleDown)
                                    {
                                        KeyboardSWThrottleDown = true;
                                    }
                                    else
                                    {
                                        KeyboardSWThrottleDown = false;
                                    }
                                }
                                else
                                {
                                    if (!KeyboardSWThrottleUp)
                                    {
                                        KeyboardSWThrottleUp = true;
                                    }
                                    else
                                    {
                                        KeyboardSWThrottleUp = false;
                                    }
                                }
                                break;
                            }
                            else
                            {
                                if (KeyboardSWThrottleActivate)
                                {
                                    KeyboardSWThrottleActivate = false;
                                }
                            }
                        }
                        else
                        {
                            if (joystickButtons.ButtonName.Equals("Throttle Brake"))
                            {
                                break;
                            }
                        }
                        return gas;
                }
                case AnalogType.Brake:
                {
                    var brake = HandleGasBrakeForJvs(state.Value, joystickButtons.DirectInputButton?.IsAxisMinus, Lazydata.ParrotData.ReverseAxisGas, Lazydata.ParrotData.FullAxisGas, false);
                    if (InputCode.ButtonMode == EmulationProfile.NamcoWmmt5)
                    {
                        brake /= 3;
                    }
                        //Console.WriteLine("Brake: " + brake.ToString("X2"));
                        if (KeyboardorButtonAxis)
                        {
                            if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                            {
                                if (!KeyboardBrakeActivate)
                                {
                                    KeyboardBrakeActivate = true;
                                }
                                if (!KeyboardBrakeDown)
                                {
                                    KeyboardBrakeDown = true;
                                }
                                else
                                {
                                    KeyboardBrakeDown = false;
                                }
                                break;
                            }
                            else
                            {
                                if (KeyboardBrakeActivate)
                                {
                                    KeyboardBrakeActivate = false;
                                }
                            }
                        }  
                        return brake;
                }
                case AnalogType.Wheel:
                    {
                        var wheelPos = Lazydata.ParrotData.UseSto0ZDrivingHack
                            ? JvsHelper.CalculateSto0ZWheelPos(state.Value, Lazydata.ParrotData.StoozPercent)
                            : JvsHelper.CalculateWheelPos(state.Value, false, false, minValWheel, maxValWheel);

                        if (KeyboardorButtonAxis)
                        {
                            if (joystickButtons.ButtonName.Equals("Wheel Axis") || joystickButtons.ButtonName.Equals("Leaning Axis") || joystickButtons.ButtonName.Equals("Handlebar Axis"))
                            {
                                break;
                            }

                            if ((joystickButtons.BindNameDi.Contains("Keyboard")) || (joystickButtons.BindNameDi.Contains("Buttons")))
                            {
                                if (!KeyboardWheelActivate)
                                {
                                    KeyboardWheelActivate = true;
                                }
                                if (joystickButtons.ButtonName.Equals("Wheel Axis Right") || joystickButtons.ButtonName.Equals("Leaning Axis Right"))
                                {
                                    if (!KeyboardWheelRight)
                                    {
                                        KeyboardWheelRight = true;
                                    }
                                    else
                                    {
                                        KeyboardWheelRight = false;
                                    }
                                }
                                else if (joystickButtons.ButtonName.Equals("Wheel Axis Left") || joystickButtons.ButtonName.Equals("Leaning Axis Left"))
                                {
                                    if (!KeyboardWheelLeft)
                                    {
                                        KeyboardWheelLeft = true;
                                    }
                                    else
                                    {
                                        KeyboardWheelLeft = false;
                                    }
                                }

                                if (joystickButtons.ButtonName.Equals("Handlebar Axis Right"))
                                {
                                    if (!KeyboardHandlebarRight)
                                    {
                                        KeyboardHandlebarRight = true;
                                    }
                                    else
                                    {
                                        KeyboardHandlebarRight = false;
                                    }
                                }
                                else if (joystickButtons.ButtonName.Equals("Handlebar Axis Left"))
                                {
                                    if (!KeyboardHandlebarLeft)
                                    {
                                        KeyboardHandlebarLeft = true;
                                    }
                                    else
                                    {
                                        KeyboardHandlebarLeft = false;
                                    }
                                }
                                break;
                            }
                            else
                            {
                                if (KeyboardWheelActivate)
                                {
                                    KeyboardWheelActivate = false;
                                }
                            }
                        }
                        else
                        {
                            if (joystickButtons.ButtonName.Equals("Wheel Axis Left") || joystickButtons.ButtonName.Equals("Wheel Axis Right") || joystickButtons.ButtonName.Equals("Leaning Axis Left") || joystickButtons.ButtonName.Equals("Leaning Axis Right") ||
                                joystickButtons.ButtonName.Equals("Handlebar Axis Left") || joystickButtons.ButtonName.Equals("Handlebar Axis Right"))
                            {
                                break;
                            }
                        }
                        
                        return wheelPos;
                    }
                case AnalogType.Minimum:
                    if (state.Value == 0x80)
                    {
                        return 0x00;
                    }
                    else
                    {
                        return 0x7F;
                    }
                case AnalogType.Maximum:
                    if (state.Value == 0x80)
                    {
                        return 0xFF;
                    }
                    else
                    {
                        return 0x7F;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return null;
        }

        public byte HandleGasBrakeForJvs(int value, bool? isAxisMinus, bool isReverseAxis, bool isFullAxis, bool isGas)
        {
            if (isFullAxis)
            {
                return JvsHelper.CalculateGasPos(value, true, isReverseAxis, _gameProfile.GasAxisMin, _gameProfile.GasAxisMax);
            }

            // Dual Axis
            if (isAxisMinus.HasValue && isAxisMinus.Value)
            {
                if (value <= short.MaxValue)
                {
                    if (isGas)
                    {
                        return JvsHelper.CalculateGasPos(-value + short.MaxValue, false, isReverseAxis, _gameProfile.GasAxisMin, _gameProfile.GasAxisMax);
                    }
                    return JvsHelper.CalculateGasPos(-value + short.MaxValue, false, isReverseAxis, _gameProfile.GasAxisMin, _gameProfile.GasAxisMax);
                }
                return 0;
            }

            if (value <= short.MaxValue)
            {
                return 0;
            }

            return JvsHelper.CalculateGasPos(value + short.MaxValue, false, isReverseAxis, _gameProfile.GasAxisMin, _gameProfile.GasAxisMax);
        }
    }
}
