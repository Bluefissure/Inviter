﻿using Dalamud.Plugin;

namespace Inviter.Gui
{
    public abstract class Window<T> where T : IDalamudPlugin
    {
        protected bool WindowVisible;
        public virtual bool Visible
        {
            get => WindowVisible;
            set => WindowVisible = value;
        }

        protected T Plugin { get; }

        protected Window(T plugin)
        {
            Plugin = plugin;
        }

        public void Draw()
        {
            if (Visible)
                DrawUi();
        }

        protected abstract void DrawUi();
    }
}