using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TeknoParrotUi.Common
{
    public enum InputApi
    {
        DirectInput,
        XInput,
        RawInput
    }

    [Serializable]
    [XmlRoot("GameProfile")]
    public class GameProfile
    {
        public string GameName { get; set; }
        public string GamePath { get; set; }
        public string ExtraParameters { get; set; }
        public string IconName { get; set; }
        public bool ResetHint { get; set; }
        public string InvalidFiles { get; set; }
        public string Description { get; set; }
        [XmlIgnore]
        public Description GameInfo { get; set; }
        [XmlIgnore]
        public string FileName { get; set; }
        public List<FieldInformation> ConfigValues { get; set; }
        public List<JoystickButtons> JoystickButtons { get; set; }
        public EmulationProfile EmulationProfile { get; set; }
        public bool Is64Bit { get; set; }
        public bool TestExecIs64Bit { get; set; }
        public EmulatorType EmulatorType { get; set; }
        public bool RequiresAdmin { get; set; }
        public int msysType { get; set; }
        public string ExecutableName { get; set; }
        public string ExecutableName2 { get; set; }
        public string GameVersion { get; set; }
        public bool HasTwoExecutables { get; set; } = true;
        public bool HasThreeExecutables { get; set; } = true;
        public bool LaunchSecondExecutableFirst { get; set; } = true;
        public string SecondExecutableArguments { get; set; }
        public string GamePath2 { get; set; }
        // advanced users only!
        public string CustomArguments { get; set; }
        public short xAxisMin { get; set; } = 0;
        public short xAxisMax { get; set; } = 255;
        public short yAxisMin { get; set; } = 0;
        public short yAxisMax { get; set; } = 255;
        public byte GasAxisMin { get; set; } = 0;
        public byte GasAxisMax { get; set; } = 255;

        public override string ToString()
        {
            return GameName;
        }
    }
}
