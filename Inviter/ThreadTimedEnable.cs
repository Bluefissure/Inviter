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
    class ThreadTimedEnable
    {
        internal Inviter plugin;
        internal long runUntil = 0;
        internal long lastNotification = 0;
        internal bool isRunning = false;

        public ThreadTimedEnable(Inviter plugin)
        {
            this.plugin = plugin;
        }

        public void Run()
        {
            isRunning = true;
            lastNotification = DateTimeOffset.Now.ToUnixTimeSeconds();
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
                    if(DateTimeOffset.Now.ToUnixTimeSeconds() - lastNotification >= 60
                        && runUntil - DateTimeOffset.Now.ToUnixTimeSeconds() > 30)
                    {
                        plugin.Interface.Framework.Gui.Toast.ShowQuest("Automatic recruitment enabled, "+
                            Math.Round((runUntil - DateTimeOffset.Now.ToUnixTimeSeconds()) / 60d)
                            + " minutes left");
                        lastNotification = DateTimeOffset.Now.ToUnixTimeSeconds();
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
    }
}
