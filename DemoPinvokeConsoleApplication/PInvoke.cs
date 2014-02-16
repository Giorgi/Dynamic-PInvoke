﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DemoPinvokeConsoleApplication
{
    class PInvoke
    {
        const string Beep = "MessageBeep";

        [DebuggerStepThrough]
        [DispId(0), DllImport("User32.dll", EntryPoint = "MessageBeep")]
        public extern static bool r(UInt32 beepType);

        [DllImport("User32.dll", EntryPoint = Beep, CallingConvention = CallingConvention.ThisCall)]
        public static extern bool MB(UInt32 beepType);

        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool MessageBeep(UInt32 beepType);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        public void d()
        {
            
        }

        [DllImport("shell32.dll")]
        static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);
    }
}