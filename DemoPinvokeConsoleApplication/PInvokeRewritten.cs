using System;
using System.Runtime.InteropServices;

namespace DemoPinvokeConsoleApplication
{
    class PInvokeDynamic
    {
        const string Beep = "MessageBeep";
        public void d()
        {
        }

        delegate bool rDelegate(UInt32 beepType);
        delegate bool MBDelegate(UInt32 beepType);
        delegate bool MessageBeepDelegate(UInt32 beepType);
        public static bool r(UInt32 beepType)
        {
            using (var library = new UnmanagedLibrary("User32.dll"))
            {
                var function = library.GetUnmanagedFunction<rDelegate>("MessageBeep");
                return function(beepType);
            }
        }

        public static bool MB(UInt32 beepType)
        {
            using (var library = new UnmanagedLibrary("User32.dll"))
            {
                var function = library.GetUnmanagedFunction<MBDelegate>(Beep);
                return function(beepType);
            }
        }

        public static bool MessageBeep(UInt32 beepType)
        {
            using (var library = new UnmanagedLibrary("User32.dll"))
            {
                var function = library.GetUnmanagedFunction<MessageBeepDelegate>("MessageBeep");
                return function(beepType);
            }
        }
    }
}