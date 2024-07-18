using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Game;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Network;
using Dalamud.IoC;
using GroupManager = FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace Inviter
{
    public class Inviter : IDalamudPlugin
    {
        public string Name => "Inviter";
        public PluginUi Gui { get; private set; }
        internal Gui.Localizer Localizer => Gui.ConfigWindow._localizer;
        public Configuration Config { get; private set; }

        private static object LockInviteObj = new object();

        private ulong NextInviteAt = 0;

        private delegate IntPtr GetUIBaseDelegate();
        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);
        private delegate char EasierProcessInviteDelegate(Int64 a1, Int64 a2, Int16 world_id, IntPtr name, char a5);
        private delegate char EasierProcessEurekaInviteDelegate(Int64 a1, Int64 a2);
        private delegate char EasierProcessCIDDelegate(Int64 a1, Int64 a2);
        private EasierProcessInviteDelegate _EasierProcessInvite;
        private EasierProcessEurekaInviteDelegate _EasierProcessEurekaInvite;
        private Hook<EasierProcessEurekaInviteDelegate> easierProcessEurekaInviteHook;
        private Hook<EasierProcessCIDDelegate> easierProcessCIDHook;
        private GetUIModuleDelegate GetUIModule;
        private delegate IntPtr GetMagicUIDelegate(IntPtr basePtr);
        private IntPtr uiModule;
        private Int64 uiInvite;
        private IntPtr groupManagerAddress;
        private Dictionary<string, Int64> name2CID;
        internal TimedEnable timedRecruitment;

        [PluginService]
        public static ICommandManager CmdManager { get; private set; }
        [PluginService]
        public static IFramework Framework { get; private set; }
        [PluginService]
        public static ISigScanner SigScanner { get; private set; }
        [PluginService]
        public static IDalamudPluginInterface Interface { get; private set; }
        [PluginService]
        public static IGameGui GameGui { get; private set; }
        [PluginService]
        public static IChatGui ChatGui { get; private set; }
        [PluginService]
        public static IToastGui ToastGui { get; private set; }
        [PluginService]
        public static IClientState ClientState { get; private set; }
        [PluginService]
        public static ICondition Condition { get; private set; }
        [PluginService]
        public static IDataManager Data { get; private set; }
        [PluginService]
        public static IGameInteropProvider Hook { get; private set; }
        [PluginService]
        public static IPluginLog PluginLog { get; private set; }


        public void Dispose()
        {
            if (timedRecruitment.isRunning)
            {
                timedRecruitment.runUntil = 0;
                Config.Enable = false;
            }
            easierProcessCIDHook.Dispose();
            // easierProcessEurekaInviteHook.Dispose();
            ChatGui.ChatMessage -= Chat_OnChatMessage;
            // Interface.Framework.Network.OnNetworkMessage -= Chat_OnNetworkMessage;
            ClientState.TerritoryChanged -= TerritoryChanged;
            CmdManager.RemoveHandler("/xinvite");
            Gui?.Dispose();
            // Interface?.Dispose();
        }

        public Inviter()
        {
            name2CID = new Dictionary<string, long> { };
            Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
            var InviteToPartyByName = SigScanner.ScanText("E8 ?? ?? ?? ?? EB CC CC");
            var InviteToPartyInInstance = SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B 65 88 E9 ?? ?? ?? ?? 48 8B 83 ?? ?? ?? ??");
            var easierProcessCIDPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 78");
            groupManagerAddress = SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 44 8B E7");
            InitUi();
            PluginLog.Info("===== I N V I T E R =====");
            PluginLog.Info($"InviteToPartyByName address {InviteToPartyByName:X}");
            PluginLog.Info($"InviteToPartyInInstance address {InviteToPartyInInstance:X}");
            PluginLog.Info($"Process CID address {easierProcessCIDPtr:X}");
            PluginLog.Info($"uiModule address {uiModule:X}");
            PluginLog.Info($"uiInvite address {uiInvite:X}");
            PluginLog.Info($"groupManager address {groupManagerAddress:X}");


            //Log($"EurekaInvite:{easierProcessEurekaInvitePtr}");
            //Interface.Framework.Gui.Chat.OnChatMessageRaw += Chat_OnChatMessageRaw;
            _EasierProcessInvite = Marshal.GetDelegateForFunctionPointer<EasierProcessInviteDelegate>(InviteToPartyByName);
            _EasierProcessEurekaInvite = Marshal.GetDelegateForFunctionPointer<EasierProcessEurekaInviteDelegate>(InviteToPartyInInstance);
            /*
            easierProcessEurekaInviteHook = new Hook<EasierProcessEurekaInviteDelegate>(easierProcessEurekaInvitePtr,
                                                                               new EasierProcessEurekaInviteDelegate(EasierProcessEurekaInviteDetour),
                                                                               this);
            */
            easierProcessCIDHook = Hook.HookFromAddress<EasierProcessCIDDelegate>(easierProcessCIDPtr, new EasierProcessCIDDelegate(EasierProcessCIDDetour));
            easierProcessCIDHook.Enable();
            //easierProcessEurekaInviteHook.Enable();

            CmdManager.AddHandler("/xinvite", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/xinvite - open the inviter panel.\n" +
                    "/xinvite <on/off/toggle> - turn the auto invite on/off.\n" +
                    "/xinvite <minutes> - enable temporary auto invite for certain amount of time in minutes.\n" +
                    "/xinvite <minutes> <attempts> - enable temporary auto invite for certain amount of time in minutes and finish it after certain amount of invite attempts.\n"
            });
            Gui = new PluginUi(this);
            ChatGui.ChatMessage += Chat_OnChatMessage;
            // Interface.Framework.Network.OnNetworkMessage += Chat_OnNetworkMessage;
            ClientState.TerritoryChanged += TerritoryChanged;
            timedRecruitment = new TimedEnable(this);
        }

        private void TerritoryChanged(ushort e)
        {
            List<ushort> eureka_territories = new List<ushort> { 732, 763, 795, 827, 920, 975 };
            if (eureka_territories.IndexOf(e) != -1)
            {
                Config.Eureka = true;
                Config.Save();
            }
            else
            {
                Config.Eureka = false;
                Config.Save();
            }
            name2CID.Clear();
        }

        public void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);

            if (string.IsNullOrEmpty(args) || args.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                return;
            }
            else if (args == "on")
            {
                Config.Enable = true;
                ToastGui.ShowQuest(
                        String.Format(Localizer.Localize("Auto invite is turned on for \"{0}\""), Config.TextPattern)
                    , new QuestToastOptions
                    {
                        DisplayCheckmark = true,
                        PlaySound = true
                    });
                Config.Save();
            }
            else if (args == "off")
            {
                Config.Enable = false;
                ToastGui.ShowQuest(Localizer.Localize("Auto invite is turned off"), new QuestToastOptions
                {
                    DisplayCheckmark = true,
                    PlaySound = true
                });
                Config.Save();
            }
            else if (args == "party")
            {
                unsafe
                {
                    GroupManager* groupManager = (GroupManager*)groupManagerAddress;
                    var group = groupManager->GetGroup();
                    var leader = group->GetPartyMemberByIndex((int)group->PartyLeaderIndex);
                    string leaderName = ConvertSpanToString(leader->Name);
                    Log($"MemberCount:{group->MemberCount}");
                    Log($"LeaderIndex:{group->PartyLeaderIndex}");
                    Log($"LeaderName:{leaderName}");
                    Log($"SelfName:{ClientState.LocalPlayer.Name.ToString()}");
                    Log($"isLeader:{ClientState.LocalPlayer.Name.ToString() == leaderName}");
                }
            }
            else if (args == "toggle")
            {
                Config.Enable = !Config.Enable;
                if (Config.Enable)
                {
                    ToastGui.ShowQuest(
                        String.Format(Localizer.Localize("Auto invite is turned on for \"{0}\""), Config.TextPattern)
                    , new QuestToastOptions
                    {
                        DisplayCheckmark = true,
                        PlaySound = true
                    });
                }
                else
                {
                    ToastGui.ShowQuest(Localizer.Localize("Auto invite is turned off"), new QuestToastOptions
                    {
                        DisplayCheckmark = true,
                        PlaySound = true
                    });
                }
                Config.Save();
            }
            else if (timedRecruitment.TryProcessCommandTimedEnable(args))
            {
                //success
            }
            else if(CmdManager.Commands.TryGetValue("/xinvite", out var cmdInfo))
            {
                ChatGui.Print(cmdInfo.HelpMessage);
            }
        }
        private void InitUi()
        {
            uiModule = GameGui.GetUIModule();
            if (uiModule == IntPtr.Zero)
                throw new ApplicationException("uiModule was null");
            IntPtr step2 = Marshal.ReadIntPtr(uiModule) + 280;
            PluginLog.Info($"step2:0x{step2:X}");
            if (step2 == IntPtr.Zero)
                throw new ApplicationException("step2 was null");
            IntPtr step3 = Marshal.ReadIntPtr(step2);
            PluginLog.Info($"step3:0x{step3:X}");
            if (step3 == IntPtr.Zero)
                throw new ApplicationException("step3 was null");
            IntPtr step4 = Marshal.GetDelegateForFunctionPointer<GetMagicUIDelegate>(step3)(uiModule) + 6536;
            PluginLog.Info($"step4:0x{step4:X}");
            if (step4 == (IntPtr.Zero + 6536))
                throw new ApplicationException("step4 was null");
            uiInvite = Marshal.ReadInt64(step4);
            if (uiInvite == 0)
                throw new ApplicationException("uiInvite was 0");
        }

        public void Log(string message)
        {
            if (!Config.PrintMessage) return;
            var msg = $"[{Name}] {message}";
            PluginLog.Info(msg);
            ChatGui.Print(msg);
        }
        public void LogError(string message)
        {
            if (!Config.PrintError) return;
            var msg = $"[{Name}] {message}";
            PluginLog.Error(msg);
            ChatGui.PrintError(msg);
        }
        private void Chat_OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            return;
            /*
            if (!Config.Enable || !Config.Eureka) return;
            if (direction != NetworkMessageDirection.ZoneDown)
                return;
            var client = ClientState.ClientLanguage == ClientLanguage.ChineseSimplified ? "cn" : "intl";
            // not used after hooking the function
            // https://github.com/karashiiro/MachinaWrapperJSON/blob/master/MachinaWrapper/Models/Sapphire/Ipcs.cs
            // https://github.com/karashiiro/MachinaWrapperJSON/blob/master/MachinaWrapper/Models/Sapphire/Ipcs_cn.cs
            // if not found, it'll be triggered when chatting and the length should be 1104.
            ushort chat_opcode = (ushort)(client == "cn" ? 0x021e : 0x02c2); 
            if (opCode != chat_opcode)
                return;
            byte[] managedArray = new byte[32];
            Marshal.Copy(dataPtr, managedArray, 0, 32);
            Log("Network dataPtr");
            Log(BitConverter.ToString(managedArray).Replace("-", " "));
            return;
            Int64 CID = Marshal.ReadInt64(dataPtr);
            short world_id = Marshal.ReadInt16(dataPtr, 12);
            string name = StringFromNativeUtf8(dataPtr + 16);
            Log($"{name}@{world_id}:{CID}");
            string playerNameKey = $"{name}@{world_id}";
            if (!name2CID.ContainsKey(playerNameKey))
            {
                name2CID.Add(playerNameKey, CID);
            }
            */
        }

        public static string ConvertSpanToString(Span<byte> byteSpan)
        {
            int length = 0;

            for (int i = 0; i < byteSpan.Length; i++)
            {
                if (byteSpan[i] == 0)
                {
                    break;
                }
                length++;
            }

            return Encoding.UTF8.GetString(byteSpan.Slice(0, length));
        }
        public static IntPtr NativeUtf8FromString(string managedString)
        {
            int len = Encoding.UTF8.GetByteCount(managedString);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        public static string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
        private void Chat_OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!Config.Enable) return;
            if (!Enum.IsDefined(typeof(XivChatType), type)) return;
            if (Config.FilteredChannels.IndexOf((ushort)type) != -1) return;
            if (Config.HiddenChatType.IndexOf(type) != -1) return;
            if (Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51]) return;
            var pattern = Config.TextPattern;
            bool matched = false;
            if (!Config.RegexMatch)
            {
                matched = (message.TextValue.IndexOf(pattern) != -1);
            }
            else
            {
                Regex rx = new Regex(pattern, RegexOptions.IgnoreCase);
                matched = rx.Matches(message.TextValue).Count > 0;
            }
            if (matched)
            {

                var senderPayload = sender.Payloads.Where(payload => payload is PlayerPayload).FirstOrDefault();
                if (senderPayload != default(Payload) && senderPayload is PlayerPayload playerPayload)
                {

                    if (groupManagerAddress != IntPtr.Zero)
                    {
                        unsafe
                        {
                            GroupManager* groupManager = (GroupManager*)groupManagerAddress;
                            var group = groupManager->GetGroup();
                            if (group->MemberCount >= 8)
                            {
                                Log($"Full party, won't invite.");
                                if (timedRecruitment.isRunning)
                                {
                                    timedRecruitment.runUntil = 0;
                                }
                                return;
                            }
                            else
                            {
                                if (group->MemberCount > 0)
                                {
                                    var leader = group->GetPartyMemberByIndex((int)group->PartyLeaderIndex);
                                    string leaderName = ConvertSpanToString(leader->Name);

                                    if (ClientState.LocalPlayer.Name.ToString() != leaderName)
                                    {
                                        Log($"Not leader, won't invite. (Leader: {leaderName})");
                                        return;
                                    }
                                }
                                Log($"Party Count:{group->MemberCount}");
                            }
                        }
                    }
                    var tc64 = Native.GetTickCount64();
                    if (tc64 > NextInviteAt)
                    {
                        if(timedRecruitment.isRunning && timedRecruitment.MaxInvitations > 0)
                        {
                            if(timedRecruitment.InvitationAttempts >= timedRecruitment.MaxInvitations)
                            {
                                Log($"Reached target amound of invitations, won't invite {timedRecruitment.InvitationAttempts}/{timedRecruitment.MaxInvitations}");
                                timedRecruitment.runUntil = 0;
                                return;
                            }
                            else
                            {
                                timedRecruitment.InvitationAttempts++;
                                Log($"Invitation {timedRecruitment.InvitationAttempts} out of {timedRecruitment.MaxInvitations}");
                            }
                        }
                        NextInviteAt = tc64 + (uint)Config.Ratelimit;
                        if (Config.Eureka)
                        {
                            Task.Run(() =>
                            {
                                ProcessEurekaInvite(playerPayload);
                            });
                        }
                        else
                        {
                            Task.Run(() =>
                            {
                                ProcessInvite(playerPayload);
                            });
                        }
                    }
                    else
                    {
                        Log($"Rate limiting invitation (next invite in {NextInviteAt - tc64} ms)");
                    }
                }
            }

        }

        public void ProcessInvite(PlayerPayload player)
        {
            int delay = Math.Max(0, Config.Delay);
            Thread.Sleep(delay);
            Log($"Invite:{player.PlayerName}@{player.World.Name}");
            string player_name = player.PlayerName;
            var player_bytes = Encoding.UTF8.GetBytes(player_name);
            IntPtr mem1 = Marshal.AllocHGlobal(player_bytes.Length + 1);
            Marshal.Copy(player_bytes, 0, mem1, player_bytes.Length);
            Marshal.WriteByte(mem1, player_bytes.Length, 0);
            lock (LockInviteObj)
            {
                _EasierProcessInvite(uiInvite, 0, (short)player.World.RowId, mem1, (char)1);
            }
            Marshal.FreeHGlobal(mem1);
        }

        public void ProcessEurekaInvite(PlayerPayload player)
        {
            int delay = Math.Max(500, Config.Delay); // 500ms to make sure the name2CID is updated
            Thread.Sleep(delay);
            string playerNameKey = $"{player.PlayerName}@{player.World.RowId}";
            if (!name2CID.ContainsKey(playerNameKey))
            {
                LogError($"Unable to get CID:{player.PlayerName}@{player.World.Name}");
                return;
            }
            Log($"Invite in Eureka:{player.PlayerName}@{player.World.Name}");
            Int64 CID = name2CID[playerNameKey];
            lock (LockInviteObj)
            {
                _EasierProcessEurekaInvite(uiInvite, CID);
            }
        }
        public char EasierProcessCIDDetour(Int64 a1, Int64 a2)
        {
            IntPtr dataPtr = (IntPtr)a2;
            Log($"CID hook a1: 0x{a1:X}");
            Log($"CID hook dataPtr: 0x{dataPtr:X}");
            /*
            byte[] managedArray = new byte[32];
            Marshal.Copy(dataPtr, managedArray, 0, 32);
            Log(BitConverter.ToString(managedArray).Replace("-", " "));
            */
            if (Config.Enable && Config.Eureka && dataPtr != IntPtr.Zero)
            {
                Int64 CID = Marshal.ReadInt64(dataPtr, 8);
                short world_id = Marshal.ReadInt16(dataPtr, 20);
                var world = Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>().GetRow((uint)world_id);
                string name = StringFromNativeUtf8(dataPtr + 24);
                Log($"{name}@{world.Name}:{CID}");
                string playerNameKey = $"{name}@{world_id}";
                if (!name2CID.ContainsKey(playerNameKey))
                {
                    name2CID.Add(playerNameKey, CID);
                }
            }
            return easierProcessCIDHook.Original(a1, a2);
        }

        /*
        public char EasierProcessEurekaInviteDetour(Int64 a1, Int64 a2)
        {
            Log($"EurekaInvite hook a1:{a1}");
            Log($"EurekaInvite hook a2:{a2}");
            return easierProcessEurekaInviteHook.Original(a1, a2);
        }
        */
    }
}
