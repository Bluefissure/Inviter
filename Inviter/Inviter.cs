using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inviter
{
    public class Inviter : IDalamudPlugin
    {
        public static Inviter Plugin;
        public Configuration Config { get; private set; }
        public Localizer localizer;
        public TimedEnable timedRecruitment;
        public WindowSystem WindowSystem = new("Inviter");
        public ConfigurationWindow ConfigurationWindow;

        private long NextInviteAt = 0;
        private readonly Hook<RaptureLogModule.Delegates.AddMsgSourceEntry> MsgHook;

        public unsafe Inviter(IDalamudPluginInterface PluginInterface)
        {
            Plugin = this;
            Svc.Init(PluginInterface);
            Config = Svc.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            localizer = new Localizer(Config.UILanguage);
            timedRecruitment = new();

            MsgHook = Svc.Hook.HookFromAddress<RaptureLogModule.Delegates.AddMsgSourceEntry>(RaptureLogModule.MemberFunctionPointers.AddMsgSourceEntry, MsgHookDetour);
            MsgHook.Enable();

            Svc.Commands.AddHandler("/xinvite", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/xinvite - open the inviter panel.\n" +
                    "/xinvite <on/off/toggle> - turn the auto invite on/off.\n" +
                    "/xinvite <minutes> - enable temporary auto invite for certain amount of time in minutes.\n" +
                    "/xinvite <minutes> <attempts> - enable temporary auto invite for certain amount of time in minutes and finish it after certain amount of invite attempts."
            });
            ConfigurationWindow = new();
            WindowSystem.AddWindow(ConfigurationWindow);
            Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
            Svc.PluginInterface.UiBuilder.OpenMainUi += ConfigurationWindow.Toggle;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += ConfigurationWindow.Toggle;

        }

        public void Dispose()
        {
            timedRecruitment.FinishTimer();
            MsgHook.Dispose();
            Svc.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
            Svc.PluginInterface.UiBuilder.OpenMainUi -= ConfigurationWindow.Toggle;
            Svc.PluginInterface.UiBuilder.OpenConfigUi -= ConfigurationWindow.Toggle;
            WindowSystem.RemoveAllWindows();
            Svc.Commands.RemoveHandler("/xinvite");
        }

        private unsafe void MsgHookDetour(RaptureLogModule* thisPtr, ulong contentId, ulong accountId, int messageIndex, ushort worldId, ushort chatType)
        {
            MsgHook.Original(thisPtr, contentId, accountId, messageIndex, worldId, chatType);

            if (!Config.Enable)
                return;
            if (Config.FilteredChannels.Contains((XivChatType)chatType))
                return;
            if (Config.HiddenChatType.Contains((XivChatType)chatType))
                return;
            if (Svc.ClientState.LocalPlayer == null || Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
                return;

            if (!RaptureLogModule.Instance()->GetLogMessageDetail(messageIndex, out var sender, out var rawMessage, out _, out _, out _, out _))
            {
                Log("Skipping invite: unable to get message detail.");
                return;
            }

            var message = SeString.Parse(rawMessage.AsSpan()).TextValue;
            var matched = false;
            if (!Config.RegexMatch)
            {
                matched = message.Contains(Config.TextPattern, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                try
                {
                    matched = Regex.Match(message, Config.TextPattern, RegexOptions.IgnoreCase).Success;
                }
                catch (Exception)
                {
                    LogError("Skipping invite: invalid regex pattern.");
                    return;
                }
            }

            if (matched)
            {
                if (GroupManager.Instance()->GetGroup()->MemberCount >= 8)
                {
                    Log("Skipping invite: party full.");
                    if (timedRecruitment.isRunning)
                        timedRecruitment.FinishTimer();
                    return;
                }

                if (GroupManager.Instance()->GetGroup()->MemberCount > 0 && !GroupManager.Instance()->MainGroup.IsEntityIdPartyLeader(Svc.ClientState.LocalPlayer!.EntityId))
                {
                    Log("Skipping invite: not party leader.");
                    return;
                }

                if (Svc.Party.Any(p => p.ContentId == (long)contentId))
                {
                    Log("Skipping invite: already in party.");
                    return;
                }

                if (SeString.Parse(sender.AsSpan()).Payloads.FirstOrDefault(p => p is PlayerPayload) is PlayerPayload playerPayload)
                {
                    var tc64 = Environment.TickCount64;
                    if (tc64 > NextInviteAt)
                    {
                        if (timedRecruitment.isRunning && timedRecruitment.MaxInvitations > 0)
                        {
                            if (timedRecruitment.InvitationAttempts >= timedRecruitment.MaxInvitations)
                            {
                                Log($"Reached target amound of invitations, won't invite {timedRecruitment.InvitationAttempts}/{timedRecruitment.MaxInvitations}");
                                timedRecruitment.FinishTimer();
                                return;
                            }
                            else
                            {
                                timedRecruitment.InvitationAttempts++;
                                Log($"Invitation {timedRecruitment.InvitationAttempts} out of {timedRecruitment.MaxInvitations}");
                            }
                        }
                        NextInviteAt = tc64 + Config.Ratelimit;
                        Log($"Attempting to invite {playerPayload.PlayerName}");
                        if (InInvitableInstance())
                        {
                            Task.Run(() =>
                            {
                                Task.Delay(Math.Max(0, Config.Delay));
                                InfoProxyPartyInvite.Instance()->InviteToPartyInInstanceByContentId(contentId);
                            });
                        }
                        else
                        {
                            Task.Run(() =>
                            {
                                Task.Delay(Math.Max(0, Config.Delay));
                                fixed (byte* namePtr = ToTerminatedBytes(playerPayload.PlayerName))
                                    InfoProxyPartyInvite.Instance()->InviteToParty(contentId, namePtr, (ushort)playerPayload.World.RowId);
                            });
                        }
                    }
                }
            }
        }

        public unsafe void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);

            if (string.IsNullOrEmpty(args) || args.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                ConfigurationWindow.Toggle();
                return;
            }
            else if (args == "on")
            {
                Config.Enable = true;
                Svc.Toasts.ShowQuest(string.Format(localizer.Localize("Auto invite is turned on for \"{0}\""), Config.TextPattern),
                    new QuestToastOptions
                    {
                        DisplayCheckmark = true,
                        PlaySound = true
                    });
                Config.Save();
            }
            else if (args == "off")
            {
                Config.Enable = false;
                Svc.Toasts.ShowQuest(localizer.Localize("Auto invite is turned off"),
                    new QuestToastOptions
                    {
                        DisplayCheckmark = true,
                        PlaySound = true
                    });
                Config.Save();
            }
            else if (args == "party")
            {
                Log($"MemberCount:{GroupManager.Instance()->MainGroup.MemberCount}");
                Log($"LeaderIndex:{GroupManager.Instance()->MainGroup.PartyLeaderIndex}");
                if (GroupManager.Instance()->MainGroup.MemberCount > 0)
                    Log($"LeaderName:{ConvertSpanToString(GroupManager.Instance()->MainGroup.GetPartyMemberByIndex((int)GroupManager.Instance()->MainGroup.PartyLeaderIndex)->Name)}");
                Log($"SelfName:{Svc.ClientState.LocalPlayer?.Name}");
                Log($"isLeader:{GroupManager.Instance()->MainGroup.PartyLeaderIndex == 0}");
            }
            else if (args == "toggle")
            {
                Config.Enable = !Config.Enable;
                if (Config.Enable)
                {
                    Svc.Toasts.ShowQuest(string.Format(localizer.Localize("Auto invite is turned on for \"{0}\""), Config.TextPattern),
                        new QuestToastOptions
                        {
                            DisplayCheckmark = true,
                            PlaySound = true
                        });
                }
                else
                {
                    Svc.Toasts.ShowQuest(localizer.Localize("Auto invite is turned off"),
                        new QuestToastOptions
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
            else if (Svc.Commands.Commands.TryGetValue("/xinvite", out var cmdInfo))
            {
                Svc.Chat.Print(cmdInfo.HelpMessage);
            }
        }

        public void Log(string message)
        {
            if (!Config.PrintMessage)
                return;
            var msg = $"[Inviter] {message}";
            Svc.Log.Info(msg);
            Svc.Chat.Print(msg);
        }

        public void LogError(string message)
        {
            if (!Config.PrintError)
                return;
            var msg = $"[Inviter] {message}";
            Svc.Log.Error(msg);
            Svc.Chat.PrintError(msg);
        }
        private unsafe bool InInvitableInstance() => Svc.Condition[ConditionFlag.BoundByDuty56] && Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(GameMain.Instance()->CurrentTerritoryTypeId)?.TerritoryIntendedUse.RowId is 41 or 47 or 48 or 52 or 53 or 61;

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
            return Encoding.UTF8.GetString(byteSpan[..length]);
        }

        private static byte[] ToTerminatedBytes(string s)
        {
            var utf8 = Encoding.UTF8;
            var bytes = new byte[utf8.GetByteCount(s) + 1];
            utf8.GetBytes(s, 0, s.Length, bytes, 0);
            bytes[^1] = 0;
            return bytes;
        }
    }
}
