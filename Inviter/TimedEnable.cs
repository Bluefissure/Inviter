using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inviter
{
    class TimedEnable
    {
        internal Inviter plugin;
        internal long runUntil = 0;
        internal long nextNotification = 0;
        internal bool isRunning = false;

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
                while (runUntil > DateTimeOffset.Now.ToUnixTimeSeconds())
                {
                    Thread.Sleep(1000);
                    if (!plugin.Config.Enable)
                    {
                        runUntil = 0;
                        break;
                    }
                    if(DateTimeOffset.Now.ToUnixTimeSeconds() >= nextNotification)
                    {
                        plugin.Interface.Framework.Gui.Chat.Print("Automatic recruitment enabled, "+
                            Math.Ceiling((runUntil - DateTimeOffset.Now.ToUnixTimeSeconds()) / 60d)
                            + " minutes left");
                        nextNotification = DateTimeOffset.Now.ToUnixTimeSeconds() + Math.Max(60, (runUntil - DateTimeOffset.Now.ToUnixTimeSeconds()) / 2);
                    }
                }
                plugin.Interface.Framework.Gui.Toast.ShowQuest("Automatic recruitment finished", new QuestToastOptions()
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

        internal void ProcessCommandTimedEnable(string args)
        {
            if (plugin.Config.Enable && !isRunning)
            {
                plugin.Interface.Framework.Gui.Toast.ShowError("Can't start timed recruitment because Inviter is turned on permanently");
            }
            else
            {
                try
                {
                    var time = int.Parse(args);
                    if (time > 0)
                    {
                        runUntil = DateTimeOffset.Now.ToUnixTimeSeconds() + time * 60;
                        if (!isRunning)
                        {
                            new Thread(new ThreadStart(Run)).Start();
                        }
                        plugin.Interface.Framework.Gui.Toast.ShowQuest("Commenced automatic recruitment for " + time + " minutes", new QuestToastOptions()
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
                            plugin.Interface.Framework.Gui.Toast.ShowError("Recruitment is not running; can not cancel");
                        }
                    }
                    else
                    {
                        plugin.Interface.Framework.Gui.Toast.ShowError("Time can not be negative");
                    }
                }
                catch (Exception)
                {
                    plugin.Interface.Framework.Gui.Toast.ShowError("Please enter amount of time in minutes");
                }
            }
        }
    }
}
