using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Chat;
using ImGuiNET;

namespace Inviter.Gui
{
    public class ConfigurationWindow : Window<Inviter>
    {

        public Configuration Config => Plugin.Config;
        private readonly string[] _languageList;
        private int _selectedLanguage;
        private Localizer _localizer;

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

        public ConfigurationWindow(Inviter plugin) : base(plugin)
        {
            _languageList = new string[] { "en", "zh" };
            _localizer = new Localizer(Config.UILanguage);
        }

        protected override void DrawUi()
        {
            ImGui.SetNextWindowSize(new Vector2(530, 450), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin($"{Plugin.Name} {_localizer.Localize("Panel")}", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.End();
                return;
            }
            if (ImGui.BeginChild("##SettingsRegion"))
            {
                if (ImGui.CollapsingHeader(_localizer.Localize("General Settings"), ImGuiTreeNodeFlags.DefaultOpen))
                    DrawGeneralSettings();
                if (ImGui.CollapsingHeader(_localizer.Localize("Filters")))
                    DrawFilters();
                ImGui.EndChild();
            }
            ImGui.End();
        }



        private void DrawGeneralSettings()
        {
            if (ImGui.Checkbox(_localizer.Localize("Enable"), ref Config.Enable)) Config.Save();
            if (Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Automatically invite people to your party (doesn't work for CWLS/Eureka/Bozjan)."));
            ImGui.SameLine(ImGui.GetColumnWidth() - 120);
            ImGui.TextUnformatted(_localizer.Localize("Tooltips"));
            ImGui.AlignTextToFramePadding();
            ImGui.SameLine();
            if (ImGui.Checkbox("##hideTooltipsOnOff", ref Config.ShowTooltips)) Config.Save();

            ImGui.TextUnformatted(_localizer.Localize("Language:"));
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Change the UI Language."));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("##hideLangSetting", ref _selectedLanguage, _languageList, _languageList.Length))
            {
                Config.UILanguage = _languageList[_selectedLanguage];
                if (Config.UILanguage == "en")
                    Config.TextPattern = "inv";
                if (Config.UILanguage == "zh")
                    Config.TextPattern = "111";
                _localizer.Language = Config.UILanguage;
                Config.Save();
            }

            ImGui.TextUnformatted(_localizer.Localize("Pattern:"));
            if (Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Pattern of the chat message to trigger the invitation."));
            if (ImGui.InputText("##textPattern", ref Config.TextPattern, 256)) Config.Save();

            ImGui.SameLine(ImGui.GetColumnWidth() - 120);
            ImGui.TextUnformatted(_localizer.Localize("Regex"));
            ImGui.AlignTextToFramePadding();
            ImGui.SameLine();
            if (ImGui.Checkbox("##regexMatch", ref Config.RegexMatch)) Config.Save();
            if (Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(_localizer.Localize("Use regex to match the pattern to chat messages."));

            if (ImGui.Checkbox(_localizer.Localize("Print Debug Message"), ref Config.PrintMessage)) Config.Save();
            if (ImGui.Checkbox(_localizer.Localize("Print Error Message"), ref Config.PrintError)) Config.Save();

        }

        private void DrawFilters()
        {
            ImGui.Columns(4, "FiltersTable", true);
            foreach (ushort chatType in Enum.GetValues(typeof(XivChatType)))
            {
                if (HiddenChatType.IndexOf((XivChatType)chatType) != -1) continue;
                string chatTypeName = Enum.GetName(typeof(XivChatType), chatType);
                bool checkboxClicked = Config.FilteredChannels.IndexOf(chatType) == -1;
                if (ImGui.Checkbox(_localizer.Localize(chatTypeName) + "##filter", ref checkboxClicked))
                {
                    Config.FilteredChannels = Config.FilteredChannels.Distinct().ToList();
                    if (checkboxClicked)
                    {
                        if (Config.FilteredChannels.IndexOf(chatType) != -1)
                            Config.FilteredChannels.Remove(chatType);
                    }
                    else if (Config.FilteredChannels.IndexOf(chatType) == -1)
                    {
                        Config.FilteredChannels.Add(chatType);
                    }
                    Config.FilteredChannels = Config.FilteredChannels.Distinct().ToList();
                    Config.FilteredChannels.Sort();
                    Config.Save();
                }
                ImGui.NextColumn();
            }
            ImGui.Columns(1);

        }

    }
}