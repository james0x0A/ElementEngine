﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace ElementEngine
{
    public class ElementGlobals
    {
        protected static bool _loaded = false;

        // Graphics resources
        public static Sdl2Window Window;
        public static GraphicsDevice GraphicsDevice;
        public static CommandList CommandList;
        public static Viewport Viewport;

        internal static SpriteBatch2D ScreenSpaceSpriteBatch2D;
        internal static List<Action> ScreenSpaceDrawList = new List<Action>();

        // Target resolution
        public static Vector2I TargetResolutionSize => new Vector2I(TargetResolutionWidth, TargetResolutionHeight);
        public static Vector2 TargetResolutionSizeF => new Vector2(TargetResolutionWidth, TargetResolutionHeight);
        public static int TargetResolutionWidth => Window.Width;
        public static int TargetResolutionHeight => Window.Height;

        // Engine systems
        public static BaseGame Game;

        // UI
        public static Dictionary<string, Type> UIWidgetTypes { get; set; } = new Dictionary<string, Type>()
        {
            { "Button", typeof(UI.UIWBasicButton) },
            { "ImageButton", typeof(UI.UIWImageButton) },
            { "Label", typeof(UI.UIWLabel) },
            { "TextBox", typeof(UI.UIWTextBox) },
            { "ImageBox", typeof(UI.UIWImageBox) },
            { "HScrollBar", typeof(UI.UIWHScrollBar) },
            { "VScrollBar", typeof(UI.UIWVScrollBar) },
            { "HProgressBar", typeof(UI.UIWHProgressBar) },
            { "CheckBox", typeof(UI.UIWCheckbox) },
        };

        public static void RegisterWidgetType(Type type, string elementName)
        {
            if (UIWidgetTypes.ContainsValue(type))
                throw new Exception("Type already exists.");

            if (UIWidgetTypes.ContainsKey(elementName))
                throw new Exception("Element name already exists.");

            UIWidgetTypes.Add(elementName, type);
        }

        protected ElementGlobals() { }

        public static void Load(BaseGame game)
        {
            Game = game;
            _loaded = true;
        }

        public static void Unload()
        {
            if (!_loaded)
                return;

            CommandList?.Dispose();
            CommandList = null;

            GraphicsDevice?.Dispose();
            GraphicsDevice = null;

            ScreenSpaceSpriteBatch2D?.Dispose();
            ScreenSpaceSpriteBatch2D = null;

            _loaded = false;
        } // Unload

        internal static void TryRegisterScreenSpaceDraw(Action action)
        {
            if (!ScreenSpaceDrawList.Contains(action))
                ScreenSpaceDrawList.Add(action);

            if (ScreenSpaceSpriteBatch2D == null)
                ScreenSpaceSpriteBatch2D = new SpriteBatch2D();
        } // TryRegisterScreenSpaceDraw

        public static void ResetFramebuffer(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = CommandList;

            commandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        }

        public static void ResetViewport(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = CommandList;

            commandList.SetViewport(0, Viewport);
        }
    } // ElementGlobals
}
