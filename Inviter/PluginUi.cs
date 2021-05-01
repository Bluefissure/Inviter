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
            _plugin.Interface.UiBuilder.OnBuildUi += Draw;
            _plugin.Interface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;
        }

        private void Draw()
        {
            ConfigWindow.Draw();
        }
        private void OnOpenConfigUi(object sender, EventArgs args)
        {
            ConfigWindow.Visible = true;
        }

        public void Dispose()
        {
            _plugin.Interface.UiBuilder.OnBuildUi -= Draw;
            _plugin.Interface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
        }
    }
}