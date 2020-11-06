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

namespace Inviter
{
    public class Inviter : IDalamudPlugin
    {
        public string Name => "Inviter";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }
        public Configuration Config { get; private set; }

        private delegate void EasierProcessInviteDelegate(Int64 a1, Int64 a2, Int16 world_id, IntPtr name, char unknown);
        // private delegate void EasierProcessCWInviteDelegate(Int64 a1, Int64 a2, Int16 world_id, char unknown);
        private EasierProcessInviteDelegate _EasierProcessInvite;
        // private EasierProcessCWInviteDelegate _EasierProcessCWInvite;

        public void Dispose()
        {
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
            var easierProcessInvitePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? EB 19 48 8B 4B 08");
            //var easierProcessCWInvitePtr = Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 7E 08 85 FF");
            //GetUIModule = Marshal.GetDelegateForFunctionPointer<GetUIModuleDelegate>(getUIModulePtr);
            PluginLog.Log("===== I N V I T E R =====");
            PluginLog.Log("Process Invite address {Address}", easierProcessInvitePtr);
            //Log($"CWInvite:{easierProcessCWInvitePtr}");
            //Interface.Framework.Gui.Chat.OnChatMessageRaw += Chat_OnChatMessageRaw;
            _EasierProcessInvite = Marshal.GetDelegateForFunctionPointer<EasierProcessInviteDelegate>(easierProcessInvitePtr);

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
            Marshal.WriteByte(player_bytes, player_bytes.Length, 0);
            this._EasierProcessInvite(0, 0, (short)player.World.RowId, mem1, (char)1);
            Marshal.FreeHGlobal(mem1);
        }
    }
}
