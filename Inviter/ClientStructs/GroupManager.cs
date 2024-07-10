using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Inviter.ClientStructs
{
    // Referred from https://github.com/aers/FFXIVClientStructs/blob/18faa14/FFXIVClientStructs/FFXIV/Client/Game/Group/GroupManager.cs
    [StructLayout(LayoutKind.Explicit, Size = 0x65B0)]
    public unsafe struct GroupManager
    {
        [FieldOffset(0x0)] public fixed byte PartyMembers[0x3A0 * 8]; // PartyMember type
        [FieldOffset(0x6598)] public uint PartyLeaderIndex; // index of party leader in array
        [FieldOffset(0x659C)] public byte MemberCount;
    }
}
