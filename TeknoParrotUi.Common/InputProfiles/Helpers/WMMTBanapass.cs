using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TeknoParrotUi.Common.InputProfiles.Helpers
{
    class WMMTBanapass
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public static void TapBanapass()
        {
            int KEYEVENTF_KEYDOWN = 0x0001; // Key down flag
            int KEYEVENTF_KEYUP = 0x0002; // Key up flag
            byte VK = 0xE0; // Virtual key code

            // OpenBanapass uses a nice and simple keyboard press event
            // so we can replicate it by dummy press the button. :)
            keybd_event(VK, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK, 0, KEYEVENTF_KEYUP, 0);
        }
    }
}
