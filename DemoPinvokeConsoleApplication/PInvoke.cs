using System;
using System.Runtime.InteropServices;

namespace DemoPinvokeConsoleApplication
{
    class PInvoke
    {
        const string Beep = "MessageBeep";

        [DefaultDllImportSearchPaths(DllImportSearchPath.ApplicationDirectory)]
        [DispId(0), DllImport("User32.dll", EntryPoint = "MessageBeep")]
        public extern static bool r(UInt32 beepType);

        [DllImport("User32.dll", EntryPoint = Beep)]
        public static extern bool MB(UInt32 beepType);

        [DllImport("User32.dll")]
        public static extern bool MessageBeep(UInt32 beepType);

        public void d()
        {
            
        }
    }
}