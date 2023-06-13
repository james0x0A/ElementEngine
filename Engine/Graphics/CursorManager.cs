﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ElementEngine
{
    public class Cursor
    {
        public string Name { get; set; }
        public Sprite Sprite { get; set; }
        public Vector2 Offset;

        public Cursor(string name, string assetName, Vector2? offset = null)
        {
            Name = name;
            Sprite = new Sprite(AssetManager.Instance.LoadTexture2D(assetName));

            if (offset.HasValue)
                Offset = offset.Value;
            else
                Offset = Vector2.Zero;
        }
    }

    public static class CursorManager
    {
        internal static bool _registered = false;

        public static Dictionary<string, Cursor> Cursors { get; set; } = new Dictionary<string, Cursor>();
        public static Cursor CurrentCursor { get; set; }

        public static void AddCursor<T>(T name, string assetName, Vector2? offset = null)
        {
            ElementGlobals.TryRegisterScreenSpaceDraw(Draw);

            var newCursor = new Cursor(name.ToString(), assetName, offset);
            Cursors.Add(newCursor.Name, newCursor);
            CurrentCursor = newCursor;
        }

        public static void SetCursor<T>(T name)
        {
            CurrentCursor = Cursors[name.ToString()];
            ElementGlobals.Window.CursorVisible = false;
        }

        public static void Disable()
        {
            CurrentCursor = null;
            ElementGlobals.Window.CursorVisible = true;
        }

        public static void Draw()
        {
            if (CurrentCursor == null)
                return;

            CurrentCursor.Sprite.Draw(
                ElementGlobals.ScreenSpaceSpriteBatch2D,
                InputManager.MousePosition + CurrentCursor.Offset);
        }
    }
}
