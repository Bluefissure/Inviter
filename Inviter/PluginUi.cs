using System;
using Inviter.Gui;

namespace Inviter
{
    public class PluginUi : IDisposable
    {
        private readonly Inviter _plugin;
        public ConfigurationWindow ConfigWindow { get; }

        public PluginUi(Inviter plugin)
        {
            ConfigWindow = new ConfigurationWindow(plugin);

            _plugin = plugin;
            Inviter.Interface.UiBuilder.Draw += Draw;
            Inviter.Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        }

        private void Draw()
        {
            ConfigWindow.Draw();
        }
        private void OnOpenConfigUi()
        {
            ConfigWindow.Visible = true;
        }

        public void Dispose()
        {
            Inviter.Interface.UiBuilder.Draw -= Draw;
            Inviter.Interface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        }
    }
}