using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inviter.Gui;

namespace Inviter
{
    class TimedEnable
    {
        internal Inviter plugin;
        internal long runUntil = 0;
        internal long nextNotification = 0;
        internal bool isRunning = false;
        internal Localizer Localizer => plugin.Localizer;

        internal TimedEnable(Inviter plugin)
        {
            this.plugin = plugin;
        }

        internal void Run()
        {
            isRunning = true;
            nextNotification = DateTimeOffset.Now.ToUnixTimeSeconds() + (runUntil - DateTimeOffset.Now.ToUnixTimeSeconds())/2;
            try
            {
                plugin.Config.Enable = true;
                while (DateTimeOffset.Now.ToUnixTimeSeconds() < runUntil)
                {
                    Thread.Sleep(1000);
                    if (!plugin.Config.Enable)
                    {
                        runUntil = 0;
                        break;
                    }
                    if(DateTimeOffset.Now.ToUnixTimeSeconds() >= nextNotification && DateTimeOffset.Now.ToUnixTimeSeconds() < runUntil)
                    {
                        plugin.Interface.Framework.Gui.Toast.ShowQuest(
                            String.Format(Localizer.Localize("Automatic recruitment enabled, {0} minutes left"),
                            Math.Ceiling((runUntil - DateTimeOffset.Now.ToUnixTimeSeconds()) / 60d))
                            );
                        UpdateTimeNextNotification();
                    }
                }
                plugin.Interface.Framework.Gui.Toast.ShowQuest(Localizer.Localize("Automatic recruitment finished"), new QuestToastOptions()
                {
                    DisplayCheckmark = true,
                    PlaySound = true
                }) ;
                plugin.Config.Enable = false;
            }
            catch (Exception e)
            {
                plugin.Interface.Framework.Gui.Chat.Print("Error: " + e.Message + "\n" + e.StackTrace);
            }
            isRunning = false;
        }

        internal void UpdateTimeNextNotification()
        {
            nextNotification = DateTimeOffset.Now.ToUnixTimeSeconds() + Math.Max(60, (runUntil - DateTimeOffset.Now.ToUnixTimeSeconds()) / 2);
        }

        internal void ProcessCommandTimedEnable(int timeInMinutes)
        {
            if (plugin.Config.Enable && !isRunning)
            {
                plugin.Interface.Framework.Gui.Toast.ShowError(Localizer.Localize(
                        "Can't start timed recruitment because Inviter is turned on permanently"
                    ));
            }
            else
            {
                try
                {
                    var time = timeInMinutes;
                    if (time > 0)
                    {
                        runUntil = DateTimeOffset.Now.ToUnixTimeSeconds() + time * 60;
                        if (isRunning) 
                        {
                            UpdateTimeNextNotification();
                        }
                        else
                        {
                            new Thread(new ThreadStart(Run)).Start();
                        }
                        plugin.Interface.Framework.Gui.Toast.ShowQuest(
                            String.Format(Localizer.Localize("Commenced automatic recruitment for {0} minutes"), time)
                            , new QuestToastOptions()
                            {
                                DisplayCheckmark = true,
                                PlaySound = true
                            });
                    }
                    else if (time == 0)
                    {
                        if (isRunning)
                        {
                            runUntil = 0;
                        }
                        else
                        {
                            plugin.Interface.Framework.Gui.Toast.ShowError(Localizer.Localize("Recruitment is not running, can not cancel"));
                        }
                    }
                    else
                    {
                        plugin.Interface.Framework.Gui.Toast.ShowError(Localizer.Localize("Time can not be negative"));
                    }
                }
                catch (Exception e)
                {
                    // plugin.Interface.Framework.Gui.Chat.Print("Error: " + e.Message + "\n" + e.StackTrace);
                    plugin.Interface.Framework.Gui.Toast.ShowError(Localizer.Localize("Please enter amount of time in minutes"));
                }
            }
        }
    }
}
