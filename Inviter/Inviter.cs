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
        // private delegate void EasierProcessCWInviteDelegate(Int64 a1, Int64 a2, Int16 world_id, char unknown);
        private EasierProcessInviteDelegate _EasierProcessInvite;
        // private Hook<EasierProcessInviteDelegate> easierProcessInviteHook;
        private GetUIModuleDelegate GetUIModule;
        private delegate IntPtr GetMagicUIDelegate(IntPtr basePtr);
        private IntPtr getUIModulePtr;
        private IntPtr uiModulePtr;
        private IntPtr uiModule;
        private Int64 uiInvite;
        // private EasierProcessCWInviteDelegate _EasierProcessCWInvite;

        public void Dispose()
        {
            // easierProcessInviteHook.Dispose();
            Interface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
            Interface.CommandManager.RemoveHandler("/xinvite");
            Gui?.Dispose();
            Interface?.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Interface = pluginInterface;
            Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(pluginInterface);
            var easierProcessInvitePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? EB 3E 44 0F B7 83 ?? ?? ?? ??");
            getUIModulePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0");
            uiModulePtr = Interface.TargetModuleScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8 ?? ?? ?? ??");
            InitUi();
            PluginLog.Log("===== I N V I T E R =====");
            PluginLog.Log("Process Invite address {Address}", easierProcessInvitePtr);
            PluginLog.Log("uiModule address {Address}", uiModule);
            PluginLog.Log("uiInvite address {Address}", uiInvite);


            //Log($"CWInvite:{easierProcessCWInvitePtr}");
            //Interface.Framework.Gui.Chat.OnChatMessageRaw += Chat_OnChatMessageRaw;
            _EasierProcessInvite = Marshal.GetDelegateForFunctionPointer<EasierProcessInviteDelegate>(easierProcessInvitePtr);
            /*
            easierProcessInviteHook = new Hook<EasierProcessInviteDelegate>(easierProcessInvitePtr,
                                                                               new EasierProcessInviteDelegate(EasierProcessInviteDetour),
                                                                               this);
            easierProcessInviteHook.Enable();
            */

            Interface.CommandManager.AddHandler("/xinvite", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/xinvite - open the inviter panel."
            });
            Gui = new PluginUi(this);
            Interface.Framework.Gui.Chat.OnChatMessage += Chat_OnChatMessage;
        }
        public void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);

            if (string.IsNullOrEmpty(args) || args.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                return;
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
        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!Config.Enable) return;
            if (Interface.ClientState.PartyList.Length >= 8) return;
            if (Config.FilteredChannels.IndexOf((ushort)type) != -1) return;
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
                var senderPayload = sender.Payloads.Where(payload => payload is PlayerPayload).First();
                if (senderPayload != null && senderPayload is PlayerPayload playerPayload)
                {
                    ProcessInvite(playerPayload);
                }
            }

        }

        public void ProcessInvite(PlayerPayload player)
        {
            Log($"Invite:{player.PlayerName}@{player.World.Name}");
            string player_name = player.PlayerName;
            var player_bytes = Encoding.UTF8.GetBytes(player_name);
            IntPtr mem1 = Marshal.AllocHGlobal(player_bytes.Length + 1);
            Marshal.Copy(player_bytes, 0, mem1, player_bytes.Length);
            // Log($"Name Bytes:{BitConverter.ToString(player_bytes).Replace("-", " ")}");
            Marshal.WriteByte(player_bytes, player_bytes.Length, 0);
            this._EasierProcessInvite(uiInvite, 0, mem1, (short)player.World.RowId);
            Marshal.FreeHGlobal(mem1);
        }
        public char EasierProcessInviteDetour(Int64 a1, Int64 a2, IntPtr name, Int16 world_id)
        {
            Log($"hook a1:{a1}");
            Log($"hook a2:{a2}");
            return easierProcessInviteHook.Original(a1, a2, name, world_id);
        }
    }
}
