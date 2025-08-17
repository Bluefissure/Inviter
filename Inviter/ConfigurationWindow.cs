using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Linq;

namespace Inviter
{
    public class ConfigurationWindow : Window
    {
        private readonly string[] _languageList = ["en", "zh", "fr"];
        private int _selectedLanguage;

        public ConfigurationWindow() : base($"Inviter {Inviter.Plugin.localizer.Localize("Panel")}")
        {
            _selectedLanguage = Array.IndexOf(_languageList, Inviter.Plugin.Config.UILanguage);
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new(400, 300),
                MaximumSize = new(int.MaxValue, int.MaxValue)
            };
        }

        public override void OnClose()
        {
            Inviter.Plugin.Config.Save();
        }

        public override void Draw()
        {
            if (ImGui.CollapsingHeader(Inviter.Plugin.localizer.Localize("General Settings"), ImGuiTreeNodeFlags.DefaultOpen))
                DrawGeneralSettings();
            if (ImGui.CollapsingHeader(Inviter.Plugin.localizer.Localize("Filters")))
                DrawFilters();
        }

        private void DrawGeneralSettings()
        {
            if (ImGui.Checkbox(Inviter.Plugin.localizer.Localize("Enable"), ref Inviter.Plugin.Config.Enable))
                Inviter.Plugin.Config.Save();
            if (Inviter.Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(Inviter.Plugin.localizer.Localize("Automatically invite people to your party (doesn't work for CWLS)."));

            ImGui.SameLine();
            if (ImGui.Checkbox(Inviter.Plugin.localizer.Localize("Tooltips"), ref Inviter.Plugin.Config.ShowTooltips))
                Inviter.Plugin.Config.Save();

            ImGui.TextUnformatted(Inviter.Plugin.localizer.Localize("Language:"));
            if (Inviter.Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(Inviter.Plugin.localizer.Localize("Change the UI Language."));
            ImGui.AlignTextToFramePadding();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("##hideLangSetting", ref _selectedLanguage, _languageList, _languageList.Length))
            {
                Inviter.Plugin.Config.UILanguage = _languageList[_selectedLanguage];
                if (Inviter.Plugin.Config.TextPattern == "111" || Inviter.Plugin.Config.TextPattern == "inv")
                {
                    if (Inviter.Plugin.Config.UILanguage == "zh")
                        Inviter.Plugin.Config.TextPattern = "111";
                    else
                        Inviter.Plugin.Config.TextPattern = "inv";
                }
                Inviter.Plugin.localizer.Language = Inviter.Plugin.Config.UILanguage;
                Inviter.Plugin.Config.Save();
            }

            ImGui.TextUnformatted(Inviter.Plugin.localizer.Localize("Pattern:"));
            if (Inviter.Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(Inviter.Plugin.localizer.Localize("Pattern of the chat message to trigger the invitation."));
            if (ImGui.InputText("##textPattern", ref Inviter.Plugin.Config.TextPattern, 256))
                Inviter.Plugin.Config.Save();
            ImGui.SameLine();
            if (ImGui.Checkbox(Inviter.Plugin.localizer.Localize("Regex"), ref Inviter.Plugin.Config.RegexMatch))
                Inviter.Plugin.Config.Save();
            if (Inviter.Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(Inviter.Plugin.localizer.Localize("Use regex to match the pattern to chat messages."));

            ImGui.TextUnformatted(Inviter.Plugin.localizer.Localize("Delay(ms):"));
            if (Inviter.Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(Inviter.Plugin.localizer.Localize("Delay the invitation after triggered."));
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputInt("##Delay", ref Inviter.Plugin.Config.Delay, 10, 100))
                Inviter.Plugin.Config.Save();

            ImGui.TextUnformatted(Inviter.Plugin.localizer.Localize("Rate limit (ms):"));
            if (Inviter.Plugin.Config.ShowTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip(Inviter.Plugin.localizer.Localize("How much time must pass between invitations."));
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputInt("##Ratelimit", ref Inviter.Plugin.Config.Ratelimit, 10, 100))
                Inviter.Plugin.Config.Save();

            if (ImGui.Checkbox(Inviter.Plugin.localizer.Localize("Print Debug Message"), ref Inviter.Plugin.Config.PrintMessage))
                Inviter.Plugin.Config.Save();
            if (ImGui.Checkbox(Inviter.Plugin.localizer.Localize("Print Error Message"), ref Inviter.Plugin.Config.PrintError))
                Inviter.Plugin.Config.Save();

        }

        private static void DrawFilters()
        {
            ImGui.Columns(4, "FiltersTable", true);
            foreach (XivChatType chatType in Enum.GetValues<XivChatType>())
            {
                if (Inviter.Plugin.Config.HiddenChatType.Contains(chatType))
                    continue;
                string chatTypeName = Enum.GetName(chatType)!;
                bool checkboxClicked = Inviter.Plugin.Config.FilteredChannels.IndexOf(chatType) == -1;
                if (ImGui.Checkbox(Inviter.Plugin.localizer.Localize(chatTypeName) + "##filter", ref checkboxClicked))
                {
                    Inviter.Plugin.Config.FilteredChannels = [.. Inviter.Plugin.Config.FilteredChannels.Distinct()];
                    if (checkboxClicked)
                    {
                        if (Inviter.Plugin.Config.FilteredChannels.IndexOf(chatType) != -1)
                            Inviter.Plugin.Config.FilteredChannels.Remove(chatType);
                    }
                    else if (Inviter.Plugin.Config.FilteredChannels.IndexOf(chatType) == -1)
                    {
                        Inviter.Plugin.Config.FilteredChannels.Add(chatType);
                    }
                    Inviter.Plugin.Config.FilteredChannels = [.. Inviter.Plugin.Config.FilteredChannels.Distinct()];
                    Inviter.Plugin.Config.FilteredChannels.Sort();
                    Inviter.Plugin.Config.Save();
                }
                ImGui.NextColumn();
            }
            ImGui.Columns(1);
        }
    }
}