﻿using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class PUIWHScrollBar : PUIWidget
    {
        protected AnimatedSprite _background = null;
        protected AnimatedSprite _slider = null;
        protected AnimatedSprite _sliderHover = null;

        protected int _maxValue = 100;
        protected int _minValue = 1;
        protected int _increment = 1;

        protected int _previousValue = -1;
        protected int _currentValue = 1;
        protected int _sliderIndex = 1;
        protected int _totalNotches = 1;

        public int Value
        {
            get => _currentValue;
            set
            {
                UpdateCurrentValue(value);
                UpdateSliderPosition();
            }
        }

        public float FValue
        {
            get => (((float)_currentValue - (float)_minValue) / ((float)_maxValue - (float)_minValue));
            set
            {
                var newValue = (int)(((float)_maxValue - (float)_minValue) * value + (float)_minValue);
                UpdateCurrentValue(newValue);
                UpdateSliderPosition();
            }
        }

        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public string LabelText { get; set; }
        public string LabelTemplate { get; set; }
        public RgbaByte LabelTextColor { get; set; }

        public Vector2 TextPosition { get; set; }
        protected bool _labelCenterX = false;
        protected bool _labelCenterY = false;
        protected int _labelOffsetX = 0;
        protected int _labelOffsetY = 0;

        protected int _sliderOffsetX = 0;
        protected int _sliderOffsetY = 0;

        protected float _sliderIncrementX = 0f;

        protected Vector2 _bgPosition = Vector2.Zero;
        protected Vector2 _sliderPosition = Vector2.Zero;

        public PUIWHScrollBar() { }

        ~PUIWHScrollBar()
        {
            if (_background != null)
                _background.Texture?.Dispose();

            if (_slider != null)
                _slider.Texture?.Dispose();

            if (_sliderHover != null)
                _sliderHover.Texture?.Dispose();
        }

        public override void Load(PUIFrame parent, XElement el)
        {
            Init(parent, el);

            TexturePremultiplyType preMultiplyAlpha = TexturePremultiplyType.None;

            var elAlpha = GetXMLAttribute("PremultiplyAlpha");
            if (elAlpha != null)
                preMultiplyAlpha = elAlpha.Value.ToEnum<TexturePremultiplyType>();

            var sliderTexture = AssetManager.LoadTexture2D(GetXMLElement("AssetNameSlider").Value, preMultiplyAlpha);
            var sliderTextureHover = AssetManager.LoadTexture2D(GetXMLElement("AssetNameSliderHover").Value, preMultiplyAlpha);
            _slider = new AnimatedSprite(sliderTexture, sliderTexture.Size);
            _sliderHover = new AnimatedSprite(sliderTextureHover, sliderTextureHover.Size);

            var backgroundElLeft = GetXMLElement("Background", "Left");
            var backgroundElRight = GetXMLElement("Background", "Right");
            var backgroundElCenter = GetXMLElement("Background", "Center");

            var textureLeft = backgroundElLeft == null ? null : (string.IsNullOrWhiteSpace(backgroundElLeft.Value) ? null : AssetManager.LoadTexture2D(backgroundElLeft.Value, preMultiplyAlpha));
            var textureRight = backgroundElRight == null ? null : (string.IsNullOrWhiteSpace(backgroundElRight.Value) ? null : AssetManager.LoadTexture2D(backgroundElRight.Value, preMultiplyAlpha));
            var textureCenter = backgroundElCenter == null ? null : (string.IsNullOrWhiteSpace(backgroundElCenter.Value) ? null : AssetManager.LoadTexture2D(backgroundElCenter.Value, preMultiplyAlpha));

            var bgWidth = int.Parse(GetXMLElement("Background", "Width").Value);
            var backgroundTexture = new Texture2D(bgWidth, textureCenter.Height);
            
            backgroundTexture.BeginRenderTarget();
            backgroundTexture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = backgroundTexture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);

            var currentX = 0;
            var endX = bgWidth;

            if (textureLeft != null)
            {
                currentX += textureLeft.Width;
                spriteBatch.DrawTexture2D(textureLeft, new Vector2(0, 0));
            }

            if (textureRight != null)
            {
                endX = bgWidth - textureRight.Width;
                spriteBatch.DrawTexture2D(textureRight, new Vector2(endX, 0));
            }

            while (currentX < endX)
            {
                var drawWidth = textureCenter.Width;

                if ((currentX + drawWidth) > endX)
                    drawWidth = endX - currentX;

                spriteBatch.DrawTexture2D(textureCenter, new Rectangle(currentX, 0, drawWidth, textureCenter.Height));
                currentX += textureCenter.Width;
            }

            spriteBatch.End();
            backgroundTexture.EndRenderTarget();

            var atStartValue = GetXMLAttribute("StartValue");
            var atMinValue = GetXMLAttribute("MinValue");
            var atMaxValue = GetXMLAttribute("MaxValue");
            var atIncrement = GetXMLAttribute("Increment");
            var elSliderOffsetX = GetXMLElement("SliderOffsetX");

            _background = new AnimatedSprite(backgroundTexture, backgroundTexture.Size);

            if (atStartValue != null)
                _currentValue = int.Parse(atStartValue.Value);
            if (atMinValue != null)
                _minValue = int.Parse(atMinValue.Value);
            if (atMaxValue != null)
                _maxValue = int.Parse(atMaxValue.Value);
            if (atIncrement != null)
                _increment = int.Parse(atIncrement.Value);
            if (elSliderOffsetX != null)
                _sliderOffsetX = int.Parse(elSliderOffsetX.Value);

            var sliderWidthOffset = _slider.Width - (_sliderOffsetX * 2);
            Width = _background.Width + (sliderWidthOffset <= 0 ? 0 : sliderWidthOffset);
            Height = _background.Height;
            if (_slider.Height > Height)
                Height = _slider.Height;

            // center the background texture
            _bgPosition.X = (Width - _background.Width) / 2;
            _bgPosition.Y = (Height - _background.Height) / 2;

            _totalNotches = ((_maxValue - _minValue) / _increment) + 1;
            _sliderIncrementX = (Width - _slider.Width - (_sliderOffsetX * 2)) / ((_maxValue - _minValue) / _increment);

            var elLabel = GetXMLElement("Label");

            if (elLabel != null)
            {
                XElement labelPosition = GetXMLElement("Label", "Position");
                XElement labelColor = GetXMLElement("Label", "Color");

                Font = AssetManager.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
                FontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);
                LabelTextColor = new RgbaByte().FromHex(labelColor.Value);
                LabelTemplate = GetXMLElement("Label", "Template").Value;

                var labelX = labelPosition.Attribute("X").Value;
                var labelY = labelPosition.Attribute("Y").Value;

                if (labelX.ToUpper() == "CENTER")
                    _labelCenterX = true;
                else
                    _labelOffsetX = int.Parse(labelX);

                if (labelY.ToUpper() == "CENTER")
                    _labelCenterY = true;
                else
                    _labelOffsetY = int.Parse(labelY);
            }

            UpdateCurrentValue(_currentValue);
            _previousValue = _currentValue;
            UpdateSliderPosition();
        }

        protected void SetSliderPosition(Vector2 mousePosition)
        {
            var relativePosition = mousePosition - Position;
            UpdateCurrentValue((int)((relativePosition.X - _sliderOffsetX) / _sliderIncrementX) * _increment);
            UpdateSliderPosition();

            if (_previousValue != _currentValue)
            {
                //HandleEvents?.Invoke((_onValueChanged == null ? null
                //    : _onValueChanged.Replace("{value}", _currentValue.ToString()).Replace("{fvalue}", FValue.ToString("0.00")))
                //);

                _previousValue = _currentValue;
            }
        }

        protected void UpdateSliderPosition()
        {
            _sliderPosition.X = _sliderOffsetX + (_sliderIncrementX * (_sliderIndex - 1)) - (_slider.Width / 2);
        }

        protected void UpdateCurrentValue(int value)
        {
            _currentValue = value;

            if (_minValue > _currentValue)
                _currentValue = _minValue;
            if (_currentValue > _maxValue)
                _currentValue = _maxValue;

            _sliderIndex = (_currentValue / _increment) + 1;

            if (Font != null)
            {
                LabelText = LabelTemplate.Replace("{value}", _currentValue.ToString()).Replace("{fvalue}", FValue.ToString("0.00"));

                var labelSize = Font.MeasureText(LabelText, FontSize);

                int textX = (_labelCenterX == false ? _labelOffsetX : (int)((Width / 2) - (labelSize.X / 2)));
                int textY = (_labelCenterY == false ? _labelOffsetY : (int)((Height / 2) - (labelSize.Y / 2)));

                TextPosition = new Vector2(textX, textY);
            }

            TriggerPUIEvent(PUIEventType.ValueChanged);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            _background.Draw(spriteBatch, Position + _bgPosition + Parent.Position);

            if (Focused)
                _sliderHover.Draw(spriteBatch, Position + _sliderPosition + Parent.Position);
            else
                _slider.Draw(spriteBatch, Position + _sliderPosition + Parent.Position);

            if (Font != null && LabelText.Length > 0)
                spriteBatch.DrawText(Font, LabelText, TextPosition + Position + Parent.Position, LabelTextColor, FontSize);
        }

        public override void Update(GameTimer gameTimer)
        {
        }

        public override void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (!Focused && PointInsideWidget(mousePosition))
            {
                GrabFocus();
                SetSliderPosition(mousePosition);
            }
        }

        public override void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (!Focused)
                return;

            DropFocus();
        }

        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTimer gameTimer)
        {
            if (!Focused)
                return;

            SetSliderPosition(currentPosition);
        }
    } // PUIWHScrollBar
}
