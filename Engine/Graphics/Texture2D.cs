﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace ElementEngine
{
    public class Texture2D : IDisposable
    {
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;

        protected Texture _texture;
        public Texture Texture { get => _texture; }

        public TextureDescription Description { get; protected set; }

        public string TextureName { get; set; }
        public string AssetName { get; set; }

        public float TexelWidth => 1.0f / _texture.Width;
        public float TexelHeight => 1.0f / _texture.Height;
        public Vector2 TexelSize => new Vector2(TexelWidth, TexelHeight);
        public int BytesPerPixel { get; protected set; } = 4;

        public int Width { get => (int)_texture.Width; }
        public int Height { get => (int)_texture.Height; }
        public Vector2I Size { get => new Vector2I(Width, Height); }
        public Vector2 SizeF { get => Size.ToVector2(); }

        protected Framebuffer _framebuffer = null;
        protected SpriteBatch2D _renderTargetSpriteBatch2D = null;

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
                    _texture?.Dispose();
                    _framebuffer?.Dispose();
                    _renderTargetSpriteBatch2D?.Dispose();
                }

                _disposed = true;
            }
        }

        ~Texture2D()
        {
            Dispose(false);
        }
        #endregion

        public Texture2D(Texture texture, string name = null)
        {
            _texture = texture;
            Description = _texture.GetDescription();
            Setup(name);
        }

        public Texture2D(Vector2I size, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
            : this(size.X, size.Y, name, format, usage) { }

        public Texture2D(int width, int height, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
            : this((uint)width, (uint)height, name, format, usage) { }

        public Texture2D(uint width, uint height, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
        {
            _texture = GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D));
            Description = _texture.GetDescription();
            Setup(name);
            BytesPerPixel = GraphicsHelper.GetPixelFormatBytesPerPixel(format);
        }

        public Texture2D(int width, int height, RgbaByte color, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
            : this((uint)width, (uint)height, color, name, format, usage) { }

        public unsafe Texture2D(uint width, uint height, RgbaByte color, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
            : this(width, height, name, format, usage)
        {
            var data = color.ToBuffer((int)(width * height));
            GraphicsDevice.UpdateTexture(_texture, data, 0, 0, 0, width, height, 1, 0, 0);
        }

        private void Setup(string name = null)
        {
            if (name == null)
                name = Guid.NewGuid().ToString();

            _texture.Name = name;
            TextureName = name;
            AssetName = name;

        }

        public unsafe void SetData<T>(
            ReadOnlySpan<T> data, Rectangle? area = null, TexturePremultiplyType premultiplyType = TexturePremultiplyType.None)
            where T : unmanaged
        {
            Rectangle rect = area ?? new Rectangle(0, 0, Width, Height);

            fixed (T* ptr = data)
            {
                int byteCount = data.Length * Unsafe.SizeOf<T>();
                GraphicsDevice.UpdateTexture(
                    Texture, (IntPtr)ptr, (uint)byteCount,
                    (uint)rect.X, (uint)rect.Y, 0, (uint)rect.Width, (uint)rect.Height, 1, 0, 0);
            }

            // TODO: Optimize
            if (premultiplyType != TexturePremultiplyType.None)
                ApplyPremultiply(premultiplyType);
        }

        public void SetData<T>(
            Span<T> data, Rectangle? area = null, TexturePremultiplyType premultiplyType = TexturePremultiplyType.None)
            where T : unmanaged
        {
            SetData((ReadOnlySpan<T>)data, area, premultiplyType);
        }

        public void SetData<T>(
            T[] data, Rectangle? area = null, TexturePremultiplyType premultiplyType = TexturePremultiplyType.None)
            where T : unmanaged
        {
            SetData(data.AsSpan(), area, premultiplyType);
        }

        /// <summary>
        /// If the current texture already has the staging flag, it'll just return the current texture, otherwise it'll create a new staging copy.
        /// </summary>
        /// <param name="texture">The resulting staging texture.</param>
        /// <returns>True if a staging copy was created, false if the current texture was used. Use this to determine if you need to dispose the resulting texture or not.</returns>
        public bool TryCreateStagingCopy(out Texture2D texture)
        {
            if (Description.Usage.HasFlag(TextureUsage.Staging))
            {
                texture = this;
                return false;
            }

            var temp = new Texture2D(Width, Height, null, Description.Format, TextureUsage.Staging);

            var commandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            commandList.Begin();
            commandList.CopyTexture(Texture, temp.Texture);
            commandList.End();
            GraphicsDevice.SubmitCommands(commandList);
            GraphicsDevice.WaitForIdle();
            commandList.Dispose();

            texture = temp;
            return true;
        }

        public unsafe byte[] GetData()
        {
            var disposeMapTexture = TryCreateStagingCopy(out var mapTexture);

            var view = GraphicsDevice.Map<byte>(mapTexture.Texture, MapMode.Read);
            var tempData = new byte[view.SizeInBytes];
            Marshal.Copy(view.MappedResource.Data, tempData, 0, (int)view.SizeInBytes);
            GraphicsDevice.Unmap(mapTexture.Texture);

            var data = new byte[Texture.Width * Texture.Height * BytesPerPixel];
            var textureByteWidth = Texture.Width * BytesPerPixel;
            var dataIndex = 0;
            var totalRows = 0;

            while (dataIndex < data.Length)
            {
                var rowCounter = 0;

                for (var i = 0; i < view.MappedResource.RowPitch && rowCounter < textureByteWidth; i++)
                {
                    data[dataIndex] = tempData[totalRows * view.MappedResource.RowPitch + i];
                    dataIndex += 1;
                    rowCounter += 1;
                }

                totalRows += 1;
            }

            if (disposeMapTexture)
                mapTexture.Dispose();

            return data;

        }

        public Span<T> GetData<T>() where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(GetData());
        }

        public TextureView GetTextureView()
        {
            return GraphicsDevice.ResourceFactory.CreateTextureView(Texture);
        }

        public void ApplyPremultiply(TexturePremultiplyType type)
        {
            // TODO: Optimize

            if (type == TexturePremultiplyType.None)
                return;

            var data = MemoryMarshal.Cast<byte, RgbaByte>(GetData());

            for (var i = 0; i < data.Length; i++)
            {
                var color = data[i];
                float ratio = color.A / 255f;

                if (type == TexturePremultiplyType.Premultiply)
                    data[i] = new RgbaByte((byte)(color.R * ratio), (byte)(color.G * ratio), (byte)(color.B * ratio), color.A);
                else if (type == TexturePremultiplyType.UnPremultiply)
                    data[i] = new RgbaByte((byte)(color.R / ratio), (byte)(color.G / ratio), (byte)(color.B / ratio), color.A);
            }

            SetData(data);
        }

        public void SaveAsPng(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.OpenOrCreate);
            SaveAsPng(fs);
        }

        public void SaveAsPng(FileStream fs)
        {
            byte[] rawData = GetData();
            ReadOnlySpan<Rgba32> data = MemoryMarshal.Cast<byte, Rgba32>(rawData);

            // TODO: optimize by loading staging texture directly

            var image = Image.LoadPixelData(data, Width, Height);
            image.SaveAsPng(fs);
        }

        #region Render target methods
        public Framebuffer GetFramebuffer()
        {
            if (_framebuffer == null)
            {
                _framebuffer = ElementGlobals.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
                {
                    ColorTargets = new FramebufferAttachmentDescription[]
                    {
                        new FramebufferAttachmentDescription(Texture, 0),
                    },
                });
            }

            return _framebuffer;
        }

        public void BeginRenderTarget(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = ElementGlobals.CommandList;

            commandList.SetFramebuffer(GetFramebuffer());
            commandList.SetViewport(0, new Viewport(0, 0, Width, Height, 0f, 1f));
        }

        public void EndRenderTarget(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = ElementGlobals.CommandList;

            ElementGlobals.ResetFramebuffer(commandList);
            ElementGlobals.ResetViewport(commandList);
        }

        public void RenderTargetClear(RgbaFloat color, CommandList commandList = null)
        {
            if (commandList == null)
                commandList = ElementGlobals.CommandList;

            commandList.ClearColorTarget(0, color);
        }

        public SpriteBatch2D GetRenderTargetSpriteBatch2D(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = ElementGlobals.CommandList;

            if (_renderTargetSpriteBatch2D == null)
                _renderTargetSpriteBatch2D = new SpriteBatch2D(this, null, true);

            _renderTargetSpriteBatch2D.CommandList = commandList;
            return _renderTargetSpriteBatch2D;
        }
        #endregion
    }
}
