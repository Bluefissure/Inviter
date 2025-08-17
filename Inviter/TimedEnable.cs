using Dalamud.Game.Gui.Toast;
using System;
using System.Threading;

namespace Inviter
{
    public class TimedEnable
    {
        private Timer? timer;
        private long runUntil;
        private long nextNotification;
        internal volatile bool isRunning = false;
        internal uint MaxInvitations = 0;
        internal uint InvitationAttempts = 0;

        internal TimedEnable() { }

        internal void StartTimer()
        {
            if (isRunning)
                return;

            isRunning = true;
            Inviter.Plugin.Config.Enable = true;
            nextNotification = Environment.TickCount64 + (runUntil - Environment.TickCount64) / 2;
            timer = new Timer(TimerCallback, null, 0, 1000);
        }

        private void TimerCallback(object? state)
        {
            try
            {
                if (!Inviter.Plugin.Config.Enable)
                {
                    FinishTimer();
                    return;
                }

                long now = Environment.TickCount64;

                if (MaxInvitations > 0 && InvitationAttempts >= MaxInvitations)
                {
                    Svc.Toasts.ShowQuest(Inviter.Plugin.localizer.Localize("Recruitment finished: Invitation limit reached"));
                    FinishTimer();
                    return;
                }

                if (now >= runUntil)
                {
                    Svc.Toasts.ShowQuest(Inviter.Plugin.localizer.Localize("Automatic recruitment finished"),
                        new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    FinishTimer();
                    return;
                }

                if (now >= nextNotification)
                {
                    double minutesLeft = Math.Ceiling((runUntil - now) / 60d / 1000d);
                    Svc.Toasts.ShowQuest(string.Format(Inviter.Plugin.localizer.Localize("Automatic recruitment enabled, {0} minutes left"), minutesLeft));

                    long remaining = runUntil - now;
                    nextNotification = now + Math.Max(60 * 1000, remaining / 2);
                }
            }
            catch (Exception e)
            {
                Svc.Chat.Print("Error: " + e.Message + "\n" + e.StackTrace);
                FinishTimer();
            }
        }

        public void FinishTimer()
        {
            timer?.Dispose();
            timer = null;

            Inviter.Plugin.Config.Enable = false;
            Inviter.Plugin.Config.Save();
            isRunning = false;
        }

        internal bool TryProcessCommandTimedEnable(string args)
        {
            var argsArray = args.Split([" "], StringSplitOptions.RemoveEmptyEntries);
            if (argsArray.Length == 2 &&
                uint.TryParse(argsArray[0], out var time) &&
                uint.TryParse(argsArray[1], out var limit))
            {
                ProcessCommandTimedEnable(time, limit);
                return true;
            }
            else if (argsArray.Length > 0 &&
                     uint.TryParse(argsArray[0], out time))
            {
                ProcessCommandTimedEnable(time, 0);
                return true;
            }
            return false;
        }

        void ProcessCommandTimedEnable(uint timeInMinutes, uint limit)
        {
            if (Inviter.Plugin.Config.Enable && !isRunning)
            {
                Svc.Toasts.ShowError(Inviter.Plugin.localizer.Localize("Can't start timed recruitment because Inviter is turned on permanently"));
                return;
            }

            try
            {
                if (timeInMinutes == 0)
                {
                    if (isRunning)
                    {
                        FinishTimer();
                        Svc.Toasts.ShowQuest(Inviter.Plugin.localizer.Localize("Automatic recruitment canceled"),
                            new QuestToastOptions()
                            {
                                DisplayCheckmark = true,
                                PlaySound = true
                            });
                    }
                    else
                    {
                        Svc.Toasts.ShowError(Inviter.Plugin.localizer.Localize("Recruitment is not running, cannot cancel"));
                    }
                    return;
                }

                MaxInvitations = limit;
                InvitationAttempts = 0;
                runUntil = Environment.TickCount64 + timeInMinutes * 60 * 1000;

                Svc.Toasts.ShowQuest(string.Format(Inviter.Plugin.localizer.Localize("Commenced automatic recruitment for {0} minutes"), timeInMinutes),
                    new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });

                if (limit > 0)
                {
                    Svc.Toasts.ShowQuest(string.Format(Inviter.Plugin.localizer.Localize("Recruitment will finish after {0} invitation attempts"), limit),
                        new QuestToastOptions() { DisplayCheckmark = false, PlaySound = false });
                }

                if (isRunning)
                {
                    nextNotification = Environment.TickCount64 + (runUntil - Environment.TickCount64) / 2;
                }
                else
                {
                    StartTimer();
                }
            }
            catch (Exception e)
            {
                Svc.Toasts.ShowError(Inviter.Plugin.localizer.Localize("Invalid time format. Please enter minutes as a number"));
                Svc.Chat.Print("Error: " + e.Message);
            }
        }
    }
}