using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inviter.Gui;
using Dalamud.Game.Gui.Toast;

namespace Inviter
{
    class TimedEnable
    {
        internal Inviter plugin;
        internal ulong runUntil = 0;
        internal ulong nextNotification = 0;
        internal volatile bool isRunning = false;
        internal uint MaxInvitations = 0;
        internal uint InvitationAttempts = 0;
        internal Localizer Localizer => plugin.Localizer;

        internal TimedEnable(Inviter plugin)
        {
            this.plugin = plugin;
        }

        internal void Run()
        {
            isRunning = true;
            nextNotification = Native.GetTickCount64() + (runUntil - Native.GetTickCount64())/2;
            try
            {
                plugin.Config.Enable = true;
                while (Native.GetTickCount64() < runUntil)
                {
                    Thread.Sleep(1000);
                    if (!plugin.Config.Enable)
                    {
                        runUntil = 0;
                        break;
                    }
                    if(Native.GetTickCount64() >= nextNotification && Native.GetTickCount64() < runUntil)
                    {
                        Inviter.ToastGui.ShowQuest(
                            String.Format(Localizer.Localize("Automatic recruitment enabled, {0} minutes left"),
                            Math.Ceiling((runUntil - Native.GetTickCount64()) / 60d / 1000d))
                            );
                        UpdateTimeNextNotification();
                    }
                }
                Inviter.ToastGui.ShowQuest(Localizer.Localize("Automatic recruitment finished"), new QuestToastOptions()
                {
                    DisplayCheckmark = true,
                    PlaySound = true
                }) ;
                plugin.Config.Enable = false;
            }
            catch (Exception e)
            {
                Inviter.ChatGui.Print("Error: " + e.Message + "\n" + e.StackTrace);
            }
            isRunning = false;
        }

        internal void UpdateTimeNextNotification()
        {
            nextNotification = Native.GetTickCount64() + Math.Max(60 * 1000, (runUntil - Native.GetTickCount64()) / 2);
        }

        internal bool TryProcessCommandTimedEnable(string args)
        {
            var argsArray = args.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if(argsArray.Length == 2 && uint.TryParse(argsArray[0], out var time) && uint.TryParse(argsArray[1], out var limit))
            {
                ProcessCommandTimedEnable(time, limit);
                return true;
            }
            else if(uint.TryParse(argsArray[0], out time))
            {
                ProcessCommandTimedEnable(time, 0);
                return true;
            }
            return false;
        }

        void ProcessCommandTimedEnable(uint timeInMinutes, uint limit)
        {
            if (plugin.Config.Enable && !isRunning)
            {
                Inviter.ToastGui.ShowError(Localizer.Localize(
                        "Can't start timed recruitment because Inviter is turned on permanently"
                    ));
            }
            else
            {
                try
                {
                    var time = timeInMinutes; 
                    MaxInvitations = limit;
                    InvitationAttempts = 0;
                    if (time > 0)
                    {
                        runUntil = Native.GetTickCount64() + time * 60 * 1000;
                        if (isRunning) 
                        {
                            UpdateTimeNextNotification();
                        }
                        else
                        {
                            new Thread(new ThreadStart(Run)).Start();
                        }
                        Inviter.ToastGui.ShowQuest(
                            String.Format(Localizer.Localize("Commenced automatic recruitment for {0} minutes"), time)
                            , new QuestToastOptions()
                            {
                                DisplayCheckmark = true,
                                PlaySound = true
                            });
                        if(limit > 0)
                        {
                            Inviter.ToastGui.ShowQuest(
                            String.Format(Localizer.Localize("Recruitment will finish after {0} invitation attempts"), limit)
                            , new QuestToastOptions()
                            {
                                DisplayCheckmark = false,
                                PlaySound = false
                            });
                        }
                    }
                    else if (time == 0)
                    {
                        if (isRunning)
                        {
                            runUntil = 0;
                        }
                        else
                        {
                            Inviter.ToastGui.ShowError(Localizer.Localize("Recruitment is not running, can not cancel"));
                        }
                    }
                    else
                    {
                        Inviter.ToastGui.ShowError(Localizer.Localize("Time can not be negative"));
                    }
                }
                catch (Exception e)
                {
                    // plugin.Interface.Framework.Gui.Chat.Print("Error: " + e.Message + "\n" + e.StackTrace);
                    Inviter.ToastGui.ShowError(Localizer.Localize("Please enter amount of time in minutes"));
                }
            }
        }
    }
}
