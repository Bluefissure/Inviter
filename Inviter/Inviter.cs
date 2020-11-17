using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using System.Text;
using System.Threading.Tasks;
using ImGuiScene;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Resolvers;
using Dalamud.Game.Internal.Libc;
using System.Text.RegularExpressions;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Game.Internal.Network;

namespace Inviter
{
    public class Inviter : IDalamudPlugin
    {
        public string Name => "Inviter";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }
        public Configuration Config { get; private set; }

        private delegate IntPtr GetUIBaseDelegate();
        private delegate IntPtr GetUIModuleDelegate(IntPtr basePtr);
        private delegate char EasierProcessInviteDelegate(Int64 a1, Int64 a2, IntPtr name, Int16 world_id);
        private delegate char EasierProcessEurekaInviteDelegate(Int64 a1, Int64 a2);
        private delegate char EasierProcessCIDDelegate(Int64 a1, Int64 a2);
        private EasierProcessInviteDelegate _EasierProcessInvite;
        private EasierProcessEurekaInviteDelegate _EasierProcessEurekaInvite;
        private Hook<EasierProcessEurekaInviteDelegate> easierProcessEurekaInviteHook;
        private Hook<EasierProcessCIDDelegate> easierProcessCIDHook;
        private GetUIModuleDelegate GetUIModule;
        private delegate IntPtr GetMagicUIDelegate(IntPtr basePtr);
        private IntPtr getUIModulePtr;
        private IntPtr uiModulePtr;
        private IntPtr uiModule;
        private Int64 uiInvite;
        private Dictionary<string, Int64> name2CID;

        public void Dispose()
        {
            //easierProcessCIDHook.Dispose();
            //easierProcessEurekaInviteHook.Dispose();
            Interface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
            Interface.Framework.Network.OnNetworkMessage -= Chat_OnNetworkMessage;
            Interface.ClientState.TerritoryChanged -= TerritoryChanged;
            Interface.CommandManager.RemoveHandler("/xinvite");
            Gui?.Dispose();
            Interface?.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            name2CID = new Dictionary<string, long> { };
            Interface = pluginInterface;
            Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(pluginInterface);
            var easierProcessInvitePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? EB 3E 44 0F B7 83 ?? ?? ?? ??");
            var easierProcessEurekaInvitePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 83 ?? ?? ?? ?? 48 85 C0 74 62");
            var easierProcessCIDPtr = Interface.TargetModuleScanner.ScanText("40 53 48 83 EC 20 48 8B DA 48 8D 0D ?? ?? ?? ?? 8B 52 08");
            getUIModulePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
            uiModulePtr = Interface.TargetModuleScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8 ?? ?? ?? ??");
            InitUi();
            PluginLog.Log("===== I N V I T E R =====");
            PluginLog.Log("Process Invite address {Address}", easierProcessInvitePtr);
            PluginLog.Log("uiModule address {Address}", uiModule);
            PluginLog.Log("uiInvite address {Address}", uiInvite);


            //Log($"EurekaInvite:{easierProcessEurekaInvitePtr}");
            //Interface.Framework.Gui.Chat.OnChatMessageRaw += Chat_OnChatMessageRaw;
            _EasierProcessInvite = Marshal.GetDelegateForFunctionPointer<EasierProcessInviteDelegate>(easierProcessInvitePtr);
            _EasierProcessEurekaInvite = Marshal.GetDelegateForFunctionPointer<EasierProcessEurekaInviteDelegate>(easierProcessEurekaInvitePtr);
            /*
            easierProcessEurekaInviteHook = new Hook<EasierProcessEurekaInviteDelegate>(easierProcessEurekaInvitePtr,
                                                                               new EasierProcessEurekaInviteDelegate(EasierProcessEurekaInviteDetour),
                                                                               this);
            */
            //easierProcessCIDHook = new Hook<EasierProcessCIDDelegate>(easierProcessCIDPtr,
            //                                                                   new EasierProcessCIDDelegate(EasierProcessCIDDetour),
            //                                                                   this);
            //easierProcessCIDHook.Enable();
            //easierProcessEurekaInviteHook.Enable();

            Interface.CommandManager.AddHandler("/xinvite", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/xinvite - open the inviter panel."
            });
            Gui = new PluginUi(this);
            Interface.Framework.Gui.Chat.OnChatMessage += Chat_OnChatMessage;
            Interface.Framework.Network.OnNetworkMessage += Chat_OnNetworkMessage;
            Interface.ClientState.TerritoryChanged += TerritoryChanged;
        }

        private void TerritoryChanged(object sender, ushort e)
        {
            List<ushort> eureka_territories = new List<ushort> { 732, 763, 795, 827, 920 };
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
                Interface.Framework.Gui.Chat.Print($"Auto invite is turned on for \"{Config.TextPattern}\"");
                Config.Save();
            }
            else if (args == "off")
            {
                Config.Enable = false;
                Interface.Framework.Gui.Chat.Print($"Auto invite is turned off");
                Config.Save();
            }
        }
        private void InitUi()
        {
            GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);
            uiModule = GetUIModule(Marshal.ReadIntPtr(uiModulePtr));
            if (uiModule == IntPtr.Zero)
                throw new ApplicationException("uiModule was null");
            IntPtr step2 = Marshal.ReadIntPtr(uiModule) + 264;
            Log($"step2:{step2}");
            if (step2 == IntPtr.Zero)
                throw new ApplicationException("step2 was null");
            IntPtr step3 = Marshal.ReadIntPtr(step2);
            Log($"step3:{step3}");
            if (step3 == IntPtr.Zero)
                throw new ApplicationException("step3 was null");
            IntPtr step4 = Marshal.GetDelegateForFunctionPointer<GetMagicUIDelegate>(step3)(uiModule) + 6528;
            Log($"step4:{step4}");
            if (step4 == (IntPtr.Zero + 6528))
                throw new ApplicationException("step4 was null");
            uiInvite = Marshal.ReadInt64(step4);
            Log($"uiInvite:{uiInvite}");
        }

        public void Log(string message)
        {
            if (!Config.PrintMessage) return;
            var msg = $"[{Name}] {message}";
            PluginLog.Log(msg);
            Interface.Framework.Gui.Chat.Print(msg);
        }
        public void LogError(string message)
        {
            if (!Config.PrintError) return;
            var msg = $"[{Name}] {message}";
            PluginLog.LogError(msg);
            Interface.Framework.Gui.Chat.PrintError(msg);
        }
        private void Chat_OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction != NetworkMessageDirection.ZoneDown)
                return;
            var client = Interface.ClientState.ClientLanguage == ClientLanguage.ChineseSimplified ? "cn" : "intl";
            // https://github.com/karashiiro/MachinaWrapperJSON/blob/master/MachinaWrapper/Models/Sapphire/Ipcs.cs
            // https://github.com/karashiiro/MachinaWrapperJSON/blob/master/MachinaWrapper/Models/Sapphire/Ipcs_cn.cs
            // if not found, it'll be triggered when chatting and the length should be 1104.
            ushort chat_opcode = (ushort)(client == "cn" ? 0x0106 : 0x0349); 
            if (opCode != chat_opcode)
                return;
            Int64 CID = Marshal.ReadInt64(dataPtr);
            short world_id = Marshal.ReadInt16(dataPtr, 12);
            string name = StringFromNativeUtf8(dataPtr + 16);
            Log($"{name}@{world_id}:{CID}");
            name2CID.Add($"{name}@{world_id}", CID);
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
        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!Config.Enable) return;
            if (Interface.ClientState.PartyList?.Count >= 8) return; // The party list is null for now
            if (Config.FilteredChannels.IndexOf((ushort)type) != -1) return;
            if (Config.HiddenChatType.IndexOf(type) != -1) return;
            var pattern = Config.TextPattern;
            bool matched = false;
            if (!Config.RegexMatch)
            {
                matched = (message.TextValue.IndexOf(pattern) != -1);
            }
            else
            {
                Regex rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                matched = rx.Matches(message.TextValue).Count > 0;
            }
            if (matched)
            {
                var senderPayload = sender.Payloads.Where(payload => payload is PlayerPayload).FirstOrDefault();
                if (senderPayload != default(Payload) && senderPayload is PlayerPayload playerPayload)
                {
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
            _EasierProcessInvite(uiInvite, 0, mem1, (short)player.World.RowId);
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
            _EasierProcessEurekaInvite(uiInvite, CID);
        }
        /*
        public char EasierProcessCIDDetour(Int64 a1, Int64 a2)
        {
            Log($"CID hook a1:{a1}");
            Log($"CID hook a2:{a2}");
            return easierProcessCIDHook.Original(a1, a2);
        }
        public char EasierProcessEurekaInviteDetour(Int64 a1, Int64 a2)
        {
            Log($"EurekaInvite hook a1:{a1}");
            Log($"EurekaInvite hook a2:{a2}");
            return easierProcessEurekaInviteHook.Original(a1, a2);
        }
        */
    }
}
