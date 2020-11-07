using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Game.Chat;
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

        public List<ushort> FilteredChannels = new List<ushort>();


        #region Init and Save

        [NonSerialized] private DalamudPluginInterface _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }

        public void Save()
        {
            _pluginInterface.SavePluginConfig(this);
        }

        #endregion
    }
}