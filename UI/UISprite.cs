﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace ElementEngine
{
    public enum UISpriteType
    {
        Static,
        Animated,
        Auto3Slice,
        Auto9Slice,
    }

    public class UISprite : IDisposable
    {
        public static UISprite CreateUISprite(UIWidget widget, string elName)
        {
            var type = widget.GetXMLAttribute(elName, "UISpriteType").Value.ToEnum<UISpriteType>();

            switch (type)
            {
                case UISpriteType.Static:
                    return new UISpriteStatic(widget, elName);

                case UISpriteType.Animated:
                    return new UISpriteAnimated(widget, elName);

                case UISpriteType.Auto3Slice:
                    return new UISpriteAuto3Slice(widget, elName);

                case UISpriteType.Auto9Slice:
                    return new UISpriteAuto9Slice(widget, elName);

                default:
                    throw new Exception("Unsupported UISpriteType type: " + type.ToString());
            }
        } // CreateUISprite

        public TexturePremultiplyType PremultiplyType { get; protected set; } = TexturePremultiplyType.None;
        public Sprite Sprite { get; set; }

        public int Width => Sprite.Width;
        public int Height => Sprite.Height;
        public Texture2D Texture => Sprite.Texture;
        public Rectangle SourceRect => Sprite.SourceRect;

        protected void Preload(UIWidget widget, string elName)
        {
            var attPremultiplyAlpha = widget.GetXMLAttribute(elName, "PremultiplyAlpha");
            if (attPremultiplyAlpha != null)
                PremultiplyType = attPremultiplyAlpha.Value.ToEnum<TexturePremultiplyType>();
        }

        public UISprite(UIWidget widget, string elName)
        {
            Preload(widget, elName);
        }

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Sprite?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public virtual void Draw(SpriteBatch2D spriteBatch, Vector2 position)
        {
            Sprite.Draw(spriteBatch, position);
        }

        public virtual void Update(GameTimer gameTimer)
        {
            Sprite.Update(gameTimer);
        }

        public virtual void SetWidth(int width) { }
        public virtual void SetHeight(int height) { }
        public virtual void SetSize(Vector2I size) { }
    }

    public class UISpriteStatic : UISprite
    {
        ~UISpriteStatic()
        {
            Dispose(false);
        }

        public UISpriteStatic(UIWidget widget, string elName) : base(widget, elName)
        {
            Sprite = new Sprite(AssetManager.LoadTexture2D(widget.GetXMLElement(elName).Value, PremultiplyType));
        }
    } // UISpriteStatic

    public class UISpriteAnimated : UISprite
    {
        public Animation Animation { get; set; }
        public AnimatedSprite AnimatedSprite => (Sprite as AnimatedSprite);

        ~UISpriteAnimated()
        {
            Dispose(false);
        }

        public UISpriteAnimated(UIWidget widget, string elName) : base(widget, elName)
        {
            Vector2I? frameSize = null;

            var attFrameSize = widget.GetXMLAttribute(elName, "FrameSize");
            if (attFrameSize != null)
            {
                var frameSizeSplit = attFrameSize.Value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                frameSize = new Vector2I(int.Parse(frameSizeSplit[0]), int.Parse(frameSizeSplit[1]));
            }

            Sprite = new AnimatedSprite(AssetManager.LoadTexture2D(widget.GetXMLElement(elName).Value, PremultiplyType), frameSize);

            var attAnimationFrames = widget.GetXMLAttribute(elName, "Frames");
            var attAnimationDuration = widget.GetXMLAttribute(elName, "DurationPerFrame");

            if (attAnimationFrames != null && attAnimationDuration != null)
            {
                Animation = new Animation();
                Animation.SetFramesFromString(attAnimationFrames.Value);
                Animation.DurationPerFrame = float.Parse(attAnimationDuration.Value);
                AnimatedSprite.PlayAnimation(Animation);
            }
        }
    } // UISpriteAnimated

    public class UISpriteAuto3Slice : UISprite
    {
        public Texture2D TextureLeft { get; set; }
        public Texture2D TextureCenter { get; set; }
        public Texture2D TextureRight { get; set; }

        ~UISpriteAuto3Slice()
        {
            Dispose(false);
        }

        public UISpriteAuto3Slice(UIWidget widget, string elName) : base(widget, elName)
        {
            var width = 0;
            var attWidth = widget.GetXMLAttribute(elName, "Width");
            if (attWidth != null)
                width = int.Parse(attWidth.Value);

            var assetLeft = widget.GetXMLElement(elName, "Left").Value;
            var assetCenter = widget.GetXMLElement(elName, "Center").Value;
            var assetRight = widget.GetXMLElement(elName, "Right").Value;

            TextureLeft = AssetManager.LoadTexture2D(assetLeft, PremultiplyType);
            TextureCenter = AssetManager.LoadTexture2D(assetCenter, PremultiplyType);
            TextureRight = AssetManager.LoadTexture2D(assetRight, PremultiplyType);

            SetWidth(width);
        }

        public override void SetSize(Vector2I size)
        {
            SetWidth(size.X);
        }

        public override void SetWidth(int width)
        {
            if (width <= 0)
                width = 1;

            var texture = GraphicsHelper.Create3SliceTexture(width, TextureLeft, TextureCenter, TextureRight);

            Sprite?.Dispose();
            Sprite = new Sprite(texture);
        }
    } // UISpriteAuto3Slice

    public class UISpriteAuto9Slice : UISprite
    {
        public Texture2D TopTextureLeft { get; set; }
        public Texture2D TopTextureCenter { get; set; }
        public Texture2D TopTextureRight { get; set; }
        public Texture2D MiddleTextureLeft { get; set; }
        public Texture2D MiddleTextureCenter { get; set; }
        public Texture2D MiddleTextureRight { get; set; }
        public Texture2D BottomTextureLeft { get; set; }
        public Texture2D BottomTextureCenter { get; set; }
        public Texture2D BottomTextureRight { get; set; }

        ~UISpriteAuto9Slice()
        {
            Dispose(false);
        }

        public UISpriteAuto9Slice(UIWidget widget, string elName) : base(widget, elName)
        {
            var width = 0;
            var attWidth = widget.GetXMLAttribute(elName, "Width");
            if (attWidth != null)
                width = int.Parse(attWidth.Value);

            var height = 0;
            var attHeight = widget.GetXMLAttribute(elName, "Height");
            if (attHeight != null)
                height = int.Parse(attHeight.Value);

            TopTextureLeft = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "TopLeft").Value, PremultiplyType);
            TopTextureCenter = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "TopCenter").Value, PremultiplyType);
            TopTextureRight = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "TopRight").Value, PremultiplyType);

            MiddleTextureLeft = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "MiddleLeft").Value, PremultiplyType);
            MiddleTextureCenter = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "MiddleCenter").Value, PremultiplyType);
            MiddleTextureRight = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "MiddleRight").Value, PremultiplyType);

            BottomTextureLeft = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "BottomLeft").Value, PremultiplyType);
            BottomTextureCenter = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "BottomCenter").Value, PremultiplyType);
            BottomTextureRight = AssetManager.LoadTexture2D(widget.GetXMLElement(elName, "BottomRight").Value, PremultiplyType);

            SetSize(width, height);
        }

        public override void SetWidth(int width)
        {
            SetSize(width, Height);
        }

        public override void SetHeight(int height)
        {
            SetSize(Width, height);
        }

        public override void SetSize(Vector2I size)
        {
            SetSize(size.X, size.Y);
        }

        public void SetSize(int width, int height)
        {
            if (width <= 0)
                width = 1;
            if (height <= 0)
                height = 1;

            var texture = GraphicsHelper.Create9SliceTexture(
                width, height,
                TopTextureLeft, TopTextureCenter, TopTextureRight,
                MiddleTextureLeft, MiddleTextureCenter, MiddleTextureRight,
                BottomTextureLeft, BottomTextureCenter, BottomTextureRight);

            Sprite?.Dispose();
            Sprite = new Sprite(texture);
        }
    } // UISpriteAuto9Slice
}
