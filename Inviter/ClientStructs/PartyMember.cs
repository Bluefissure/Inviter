using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Inviter.ClientStructs
{
    // Referred from https://github.com/aers/FFXIVClientStructs/blob/18faa14/FFXIVClientStructs/FFXIV/Client/Game/Group/PartyMember.cs
    [StructLayout(LayoutKind.Explicit, Size = 0x3A0)]
    public unsafe struct PartyMember
    {
        [FieldOffset(0x32C)] public fixed byte Name[0x40]; // character name string
    }
}
