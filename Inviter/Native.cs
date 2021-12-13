using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inviter
{
    internal class Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern ulong GetTickCount64();
    }
}
