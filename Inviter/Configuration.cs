using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Game.Text;
using System.Linq;

namespace Inviter
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enable = true;

        public bool ShowTooltips = true;
        public string UILanguage = "en";
        public string TextPattern = "inv";
        public bool RegexMatch = false;
        public bool PrintMessage = false;
        public bool PrintError = true;
        public bool Eureka = false;
        public int Delay = 200;
        public int Ratelimit = 500;

        public List<ushort> FilteredChannels = new List<ushort>();
        public List<XivChatType> HiddenChatType = new List<XivChatType> {
            XivChatType.None,
            XivChatType.CustomEmote,
            XivChatType.StandardEmote,
            XivChatType.SystemMessage,
            XivChatType.SystemError,
            XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage,
            XivChatType.RetainerSale
        };


        #region Init and Save


        public void Save()
        {
            Inviter.Interface.SavePluginConfig(this);
        }

        #endregion
    }
}