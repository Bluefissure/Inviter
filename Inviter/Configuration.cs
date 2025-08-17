using Dalamud.Configuration;
using Dalamud.Game.Text;
using System.Collections.Generic;

namespace Inviter
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enable = false;
        public bool ShowTooltips = true;
        public string UILanguage = "en";
        public string TextPattern = "inv";
        public bool RegexMatch = false;
        public bool PrintMessage = false;
        public bool PrintError = true;
        public int Delay = 200;
        public int Ratelimit = 500;

        public List<XivChatType> FilteredChannels = [];
        public List<XivChatType> HiddenChatType = [
            XivChatType.None,
            XivChatType.CustomEmote,
            XivChatType.StandardEmote,
            XivChatType.SystemMessage,
            XivChatType.SystemError,
            XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage,
            XivChatType.RetainerSale
        ];

        public void Save()
        {
            Svc.PluginInterface.SavePluginConfig(this);
        }
    }
}