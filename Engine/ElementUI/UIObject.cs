﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIObject : IMouseHandler, IKeyboardHandler
    {
        public const int NO_DRAW_ORDER = -1;

        internal static int _nextObjectID = 0;

        public int KeyboardPriority { get; set; } = 0;
        public int MousePriority { get; set; } = 0;

        public int ObjectID = _nextObjectID++;
        public UIObject Parent;
        public UIScreen ParentScreen => this is UIScreen thisScreen ? thisScreen : (Parent == null ? null : (Parent is UIScreen screen ? screen : Parent.ParentScreen));

        public UITooltipContent? Tooltip;
        internal bool _isTooltip;

        public UIStyle Style => _style;
        public readonly List<UIObject> Children = new();
        public readonly List<UIObject> ReverseChildren = new();
        public string Name;
        public int ScrollSpeed = 15;

        public List<UIAnimation> UIAnimations = new();

        public bool IsFocused => ParentScreen.FocusedObject == this;
        public bool CanFocus = true;
        public bool IgnoreOverflow = false;
        public bool RespectMargins = false;

        public bool IsHovered { get; internal set; }

        internal int _drawOrder = NO_DRAW_ORDER;
        public int DrawOrder
        {
            get => _drawOrder;
            set
            {
                if (_drawOrder == value)
                    return;

                _drawOrder = value;

                if (Parent != null)
                    Parent.SetLayoutDirty();
            }
        }

        #region Events
        public event Action<OnHoverArgs> OnHoverEnter;
        public event Action<OnHoverArgs> OnHoverExit;
        #endregion

        #region Position, Size & Bounds
        internal bool _ignoreParentPadding;
        public bool IgnoreParentPadding
        {
            get => _ignoreParentPadding;
            set
            {
                if (_ignoreParentPadding == value)
                    return;

                _ignoreParentPadding = value;
                SetLayoutDirty();
            }
        }

        public bool HasMargin => !_margins.IsZero;
        public bool HasPadding => !_padding.IsZero;

        public Vector2I DrawPosition
        {
            get => _position + _parentOffset;
        }

        public Vector2I Position
        {
            get => _position;
        }

        public UISizeFillType? FillType
        {
            get => _uiSize.FillType;
            set
            {
                if (_uiSize.FillType == value)
                    return;

                _uiSize.FillType = value;
                SetLayoutDirty();
            }
        }

        public void SetPosition(float x, float y) => SetPosition(new Vector2(x, y));
        public void SetPosition(int x, int y) => SetPosition(new Vector2I(x, y));

        public void SetPosition(Vector2 position)
        {
            SetPosition(position.ToVector2I());
        }

        public void SetPosition(Vector2I position)
        {
            if (_uiPosition.Position == position)
                return;

            _uiPosition.Position = position;
            InternalOnPositionChanged();
            SetLayoutDirty();
        }

        public void OffsetPosition(Vector2I offset)
        {
            if (!_uiPosition.Position.HasValue)
                _uiPosition.Position = new Vector2I();

            _uiPosition.Position += offset;
            InternalOnPositionChanged();
            SetLayoutDirty();
        }

        public int X
        {
            get => _position.X;
            set
            {
                var current = _uiPosition.Position ?? Vector2I.Zero;

                if (_uiPosition.Position.HasValue && current.X == value)
                    return;

                current.X = value;
                _uiPosition.Position = current;
                InternalOnPositionChanged();
                SetLayoutDirty();
            }
        }

        public int Y
        {
            get => _position.Y;
            set
            {
                var current = _uiPosition.Position ?? Vector2I.Zero;

                if (_uiPosition.Position.HasValue && current.Y == value)
                    return;

                current.Y = value;
                _uiPosition.Position = current;
                InternalOnPositionChanged();
                SetLayoutDirty();
            }
        }

        public Vector2I Size
        {
            get => _size;
            set
            {
                if (_uiSize.Size == value)
                    return;

                _uiSize.Size = value;
                InternalOnSizeChanged();
                SetLayoutDirty();
            }
        }

        public int Width
        {
            get => _size.X;
            set
            {
                var current = _uiSize.Size ?? Vector2I.Zero;

                if (current.X == value)
                    return;

                current.X = value;
                _uiSize.Size = current;
                SetLayoutDirty();
            }
        }

        public int Height
        {
            get => _size.Y;
            set
            {
                var current = _uiSize.Size ?? Vector2I.Zero;

                if (current.Y == value)
                    return;

                current.Y = value;
                _uiSize.Size = current;
                SetLayoutDirty();
            }
        }

        public bool AutoWidth
        {
            get => _uiSize.AutoWidth;
            set
            {
                if (_uiSize.AutoWidth == value)
                    return;

                _uiSize.AutoWidth = value;
                SetLayoutDirty();
            }
        }

        public bool AutoHeight
        {
            get => _uiSize.AutoHeight;
            set
            {
                if (_uiSize.AutoHeight == value)
                    return;

                _uiSize.AutoHeight = value;
                SetLayoutDirty();
            }
        }

        public bool ParentWidth
        {
            get => _uiSize.ParentWidth;
            set
            {
                if (_uiSize.ParentWidth == value)
                    return;

                _uiSize.ParentWidth = value;
                SetLayoutDirty();
            }
        }

        public bool ParentHeight
        {
            get => _uiSize.ParentHeight;
            set
            {
                if (_uiSize.ParentHeight == value)
                    return;

                _uiSize.ParentHeight = value;
                SetLayoutDirty();
            }
        }

        public float? ParentWidthRatio
        {
            get => _uiSize.ParentWidthRatio;
            set
            {
                if (_uiSize.ParentWidthRatio == value)
                    return;

                _uiSize.ParentWidthRatio = value;
                SetLayoutDirty();
            }
        }

        public float? ParentHeightRatio
        {
            get => _uiSize.ParentHeightRatio;
            set
            {
                if (_uiSize.ParentHeightRatio == value)
                    return;

                _uiSize.ParentHeightRatio = value;
                SetLayoutDirty();
            }
        }

        public int? MinWidth
        {
            get => _uiSize.MinWidth;
            set
            {
                if (_uiSize.MinWidth == value)
                    return;

                _uiSize.MinWidth = value;
                SetLayoutDirty();
            }
        }

        public int? MaxWidth
        {
            get => _uiSize.MaxWidth;
            set
            {
                if (_uiSize.MaxWidth == value)
                    return;

                _uiSize.MaxWidth = value;
                SetLayoutDirty();
            }
        }

        public int? MinHeight
        {
            get => _uiSize.MinHeight;
            set
            {
                if (_uiSize.MinHeight == value)
                    return;

                _uiSize.MinHeight = value;
                SetLayoutDirty();
            }
        }

        public int? MaxHeight
        {
            get => _uiSize.MaxHeight;
            set
            {
                if (_uiSize.MaxHeight == value)
                    return;

                _uiSize.MaxHeight = value;
                SetLayoutDirty();
            }
        }

        public Rectangle InputBounds
        {
            get
            {
                var bounds = new Rectangle(DrawPosition, _inputSize ?? _size);

                foreach (var child in Children)
                    bounds.ExpandToContain(child.InputBounds);

                return bounds;
            }
        }

        public Rectangle Bounds
        {
            get => new Rectangle(DrawPosition, _size);
        }

        public Rectangle MarginBounds
        {
            get => new Rectangle(DrawPosition - _margins.TopLeft, _size + _margins.TopLeft + _margins.BottomRight);
        }

        public Rectangle PaddingBounds
        {
            get => new Rectangle(DrawPosition + _padding.TopLeft, _size - _padding.TopLeft - _padding.BottomRight);
        }

        internal virtual void InternalOnPositionChanged() { }
        internal virtual void InternalOnSizeChanged() { }
        #endregion

        #region Positioning

        public bool CenterX
        {
            get => _uiPosition.CenterX;
            set
            {
                _uiPosition.CenterX = value;
                SetLayoutDirty();
            }
        }

        public bool CenterY
        {
            get => _uiPosition.CenterY;
            set
            {
                _uiPosition.CenterY = value;
                SetLayoutDirty();
            }
        }

        public bool AnchorTop
        {
            get => _uiPosition.Position.HasValue ? _uiPosition.Position.Value.Y == 0 : false;
            set
            {
                if (!value)
                    return;
                if (!_uiPosition.Position.HasValue)
                    return;

                _uiPosition.Position = new Vector2I(_uiPosition.Position.Value.X, 0);
                SetLayoutDirty();
            }
        }

        public bool AnchorBottom
        {
            get => _uiPosition.AnchorBottom;
            set
            {
                _uiPosition.AnchorBottom = value;
                SetLayoutDirty();
            }
        }

        public bool AnchorLeft
        {
            get => _uiPosition.Position.HasValue ? _uiPosition.Position.Value.X == 0 : false;
            set
            {
                if (!value)
                    return;
                if (!_uiPosition.Position.HasValue)
                    return;

                _uiPosition.Position = new Vector2I(0, _uiPosition.Position.Value.Y);
                SetLayoutDirty();
            }
        }

        public bool AnchorRight
        {
            get => _uiPosition.AnchorRight;
            set
            {
                _uiPosition.AnchorRight = value;
                SetLayoutDirty();
            }
        }

        public void Center()
        {
            _uiPosition.CenterX = true;
            _uiPosition.CenterY = true;
            SetLayoutDirty();
        }
        #endregion

        #region Margins
        public int MarginLeft
        {
            get => _margins.Left;
            set
            {
                _margins.Left = value;
                SetLayoutDirty();
            }
        }

        public int MarginRight
        {
            get => _margins.Right;
            set
            {
                _margins.Right = value;
                SetLayoutDirty();
            }
        }

        public int MarginTop
        {
            get => _margins.Top;
            set
            {
                _margins.Top = value;
                SetLayoutDirty();
            }
        }

        public int MarginBottom
        {
            get => _margins.Bottom;
            set
            {
                _margins.Bottom = value;
                SetLayoutDirty();
            }
        }

        public void SetMargins(int margin)
        {
            SetMargins(margin, margin, margin, margin);
        }

        public void SetMargins(int horizontal, int vertical)
        {
            SetMargins(horizontal, horizontal, vertical, vertical);
        }

        public void SetMargins(int left, int right, int top, int bottom)
        {
            _margins.Left = left;
            _margins.Right = right;
            _margins.Top = top;
            _margins.Bottom = bottom;
            SetLayoutDirty();
        }
        #endregion

        #region Padding
        public int PaddingLeft
        {
            get => _padding.Left;
            set
            {
                _padding.Left = value;
                SetLayoutDirty();
            }
        }

        public int PaddingRight
        {
            get => _padding.Right;
            set
            {
                _padding.Right = value;
                SetLayoutDirty();
            }
        }

        public int PaddingTop
        {
            get => _padding.Top;
            set
            {
                _padding.Top = value;
                SetLayoutDirty();
            }
        }

        public int PaddingBottom
        {
            get => _padding.Bottom;
            set
            {
                _padding.Bottom = value;
                SetLayoutDirty();
            }
        }

        public void SetPadding(UISpacing padding)
        {
            SetPadding(padding.Left, padding.Right, padding.Top, padding.Bottom);
        }

        public void SetPadding(int padding)
        {
            SetPadding(padding, padding, padding, padding);
        }

        public void SetPadding(int horizontal, int vertical)
        {
            SetPadding(horizontal, horizontal, vertical, vertical);
        }

        public void SetPadding(int left, int right, int top, int bottom)
        {
            _padding.Left = left;
            _padding.Right = right;
            _padding.Top = top;
            _padding.Bottom = bottom;
            SetLayoutDirty();
        }
        #endregion

        #region Find Children
        public T FindChildByName<T>(string name, bool recursive) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t && child.Name == name)
                    return t;

                if (recursive)
                {
                    t = child.FindChildByName<T>(name, recursive);
                    if (t != null)
                        return t;
                }
            }

            return null;
        }

        public T FindChildByType<T>(bool recursive) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t)
                    return t;

                if (recursive)
                {
                    t = child.FindChildByType<T>(recursive);
                    if (t != null)
                        return t;
                }
            }

            return null;
        }

        public List<T> FindChildrenByName<T>(string name, bool recursive) where T : UIObject
        {
            var list = new List<T>();
            FindChildrenByName(name, recursive, list);
            return list;
        }

        internal void FindChildrenByName<T>(string name, bool recursive, List<T> list) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t && child.Name == name)
                    list.Add(t);

                if (recursive)
                    child.FindChildrenByName<T>(name, recursive, list);
            }
        }

        public List<T> FindChildrenByType<T>(bool recursive) where T : UIObject
        {
            var list = new List<T>();
            FindChildrenByType(recursive, list);
            return list;
        }

        internal void FindChildrenByType<T>(bool recursive, List<T> list) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t)
                    list.Add(t);

                if (recursive)
                    child.FindChildrenByType<T>(recursive, list);
            }
        }
        #endregion

        #region Visible & Active
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value)
                    Enable();
                else
                    Disable();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        public bool IsDrawn
        {
            get
            {
                if (Parent == null)
                    return IsVisible;

                if (!IsVisible)
                    return false;
                else
                    return Parent.IsDrawn;
            }
        }

        public bool IsUpdated
        {
            get
            {
                if (Parent == null)
                    return IsActive;

                if (!IsActive)
                    return false;
                else
                    return Parent.IsUpdated;
            }
        }

        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                _isHidden = value;
            }
        }

        public virtual void Show()
        {
            if (_isVisible == true)
                return;

            _isVisible = true;
            SetLayoutDirty();
        }

        public virtual void Hide()
        {
            if (_isVisible == false)
                return;

            _isVisible = false;
            SetLayoutDirty();
            HideTooltip();
        }

        public void ToggleVisible()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        public virtual void Enable()
        {
            _isActive = true;
        }

        public virtual void Disable()
        {
            _isActive = false;
            HideTooltip();
        }

        public void ToggleActive()
        {
            if (_isActive)
                Disable();
            else
                Enable();
        }

        public void ShowEnable()
        {
            Show();
            Enable();
        }

        public void HideDisable()
        {
            Hide();
            Disable();
        }
        #endregion

        internal int _childIndex;

        internal bool _isActive = true;
        internal bool _isVisible = true;
        internal bool _isHidden = false;
        internal bool _useScissorRect => _style == null ? false : (_style.OverflowType == OverflowType.Hide || _style.OverflowType == OverflowType.Scroll);
        internal bool _isScrollable => _style == null ? false : _style.OverflowType == OverflowType.Scroll;

        internal UIStyle _style;
        internal UIPosition _uiPosition;
        internal UISize _uiSize;
        internal Vector2I _position;
        internal Vector2I _childOrigin;
        internal Vector2I _childOffset;
        internal Vector2I _size;
        internal Vector2I? _inputSize;
        internal UISpacing _margins;
        internal UISpacing _padding;

        internal Vector2I? _preMarginPosition = null;
        internal Vector2I _parentOffset => Parent == null ? Vector2I.Zero : IgnoreOverflow ? Parent._parentOffset : Parent._childOffset + Parent._parentOffset;
        internal bool _layoutDirty { get; set; } = false;

        public UIObject(string name)
        {
            Name = name;
        }

        public void ApplyStyle(UIStyle style)
        {
            if (style == null)
                return;

            _style = style;
            _uiPosition = style.UIPosition ?? new UIPosition();
            _uiSize = style.UISize ?? new UISize();
            _margins = style.Margins ?? new UISpacing();
            _padding = style.Padding ?? new UISpacing();
            ScrollSpeed = style.ScrollSpeed ?? ScrollSpeed;
            IgnoreOverflow = style.IgnoreOverflow ?? IgnoreOverflow;
            IgnoreParentPadding = style.IgnoreParentPadding ?? IgnoreParentPadding;
            FillType = style.FillType ?? FillType;
        }

        public void ApplyDefaultSize(UISprite sprite)
        {
            ApplyDefaultSize(sprite.Size);
        }

        public void ApplyDefaultSize(UIObject obj)
        {
            ApplyDefaultSize(obj.Size);
        }

        public void ApplyDefaultSize(Vector2I size)
        {
            if (_uiSize.Size.HasValue)
                return;

            Width = size.X;
            Height = size.Y;
        }

        public void OverrideDefaultSize(UISprite sprite)
        {
            OverrideDefaultSize(sprite.Size);
        }

        public void OverrideDefaultSize(UIObject obj)
        {
            OverrideDefaultSize(obj.Size);
        }

        public void OverrideDefaultSize(Vector2I size)
        {
            Width = size.X;
            Height = size.Y;
        }

        #region Children
        public bool AddChild(UIObject child)
        {
            if (child == null)
                return false;

            if (Children.AddIfNotContains(child))
            {
                child._drawOrder = int.MaxValue - 1;
                child.Parent = this;
                SetLayoutDirty();

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveChild(string name)
        {
            var child = Children.Find((obj) => { return obj.Name == name; });

            if (child != null)
            {
                Children.Remove(child);
                SetLayoutDirty();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveChild(UIObject obj)
        {
            if (obj == null)
                return false;

            SetLayoutDirty();
            ReverseChildren.Clear();
            return Children.Remove(obj);
        }

        public bool RemoveChild<T>() where T : UIObject
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];

                if (child is T)
                {
                    Children.RemoveAt(i);
                    SetLayoutDirty();
                    return true;
                }
            }

            return false;
        }

        public void RemoveChildren<T>() where T : UIObject
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];

                if (child is T)
                {
                    Children.RemoveAt(i);
                    SetLayoutDirty();
                }
            }
        }

        public void ClearChildrenByType<T>() where T : UIObject
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                if (child is T)
                    Children.RemoveAt(i);
            }

            SetLayoutDirty();
        }

        public void ClearChildren()
        {
            Children.Clear();
            ReverseChildren.Clear();
            SetLayoutDirty();
        }

        public void BringToFront(UIObject child)
        {
            Children.Remove(child);
            Children.Add(child);

            for (var i = 0; i < Children.Count; i++)
                Children[i].DrawOrder = i;

            SortChildren();
            SetLayoutDirty();
        }

        public bool HasFocusedChild(bool recursive)
        {
            foreach (var child in Children)
            {
                if (child.IsFocused)
                    return true;

                if (recursive && child.HasFocusedChild(recursive))
                    return true;
            }

            return false;
        }

        internal void SortChildren()
        {
            for (var i = 0; i < Children.Count; i++)
                Children[i]._childIndex = i;

            Children.Sort((c1, c2) =>
            {
                if (c1.DrawOrder == c2.DrawOrder)
                    return c1._childIndex.CompareTo(c2._childIndex);

                return c1._drawOrder.CompareTo(c2._drawOrder);
            });

            for (var i = 0; i < Children.Count; i++)
                Children[i]._drawOrder = i;

            ReverseChildren.Clear();
            ReverseChildren.AddRange(Children);
            ReverseChildren.Sort((c1, c2) => { return c2.DrawOrder.CompareTo(c1.DrawOrder); });
        }
        #endregion

        #region Scrolling
        internal virtual void InternalOnScrollX() { }
        internal virtual void InternalOnScrollY() { }

        internal void ScrollLeft(int? amount = null)
        {
            _childOffset.X += amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollX();
        }

        internal void ScrollRight(int? amount = null)
        {
            _childOffset.X -= amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollX();
        }

        internal void ScrollUp(int? amount = null)
        {
            _childOffset.Y += amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollY();
        }

        internal void ScrollDown(int? amount = null)
        {
            _childOffset.Y -= amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollY();
        }

        internal void ClampScroll()
        {
            if (_uiSize._fullChildBounds.IsZero || this is UIScreen)
            {
                _childOffset = Vector2I.Zero;
                return;
            }

            var prevOffset = _childOffset;

            var minX = (_uiSize._fullChildBounds.Right - PaddingBounds.Width) * -1;
            var maxX = _uiSize._fullChildBounds.Left * -1;
            var minY = (_uiSize._fullChildBounds.Bottom - PaddingBounds.Height) * -1;
            var maxY = _uiSize._fullChildBounds.Top * -1;

            if (_uiSize._fullChildBounds.Width <= PaddingBounds.Width)
                _childOffset.X = _uiSize._fullChildBounds.Left * -1;
            else if (_uiSize._fullChildBounds.Right > PaddingBounds.Width)
                _childOffset.X = Math.Clamp(_childOffset.X, minX, maxX);

            if (_uiSize._fullChildBounds.Height <= PaddingBounds.Height)
                _childOffset.Y = _uiSize._fullChildBounds.Top * -1;
            else if (_uiSize._fullChildBounds.Bottom > PaddingBounds.Height)
                _childOffset.Y = Math.Clamp(_childOffset.Y, minY, maxY);
        }
        #endregion

        internal virtual void SetLayoutDirty()
        {
            //_layoutDirty = true;
            //Parent?.SetLayoutDirty();

            if (ParentScreen != null)
                ParentScreen._layoutDirty = true;
        }

        internal virtual void CheckLayout()
        {
            if (_layoutDirty)
                UpdateLayout();
        }

        internal void UpdateSizePosition()
        {
            foreach (var child in Children)
                child.UpdateSizePosition();

            UpdateSize();
            UpdatePosition();
        }

        public virtual void UpdateLayout(bool secondCheck = true, bool updateScrollbars = true)
        {
            _layoutDirty = false;

            UpdateSizePosition();

            foreach (var child in Children)
                child.UpdateLayout();

            SortChildren();
            ClampScroll();

            if (_layoutDirty && secondCheck)
            {
                UpdateLayout(false);
                _layoutDirty = false;
            }
        }

        internal void UpdateSize()
        {
            _size = _uiSize.GetSize(this);
        }

        internal void UpdatePosition()
        {
            _position = _uiPosition.GetPosition(this);
            _childOrigin = _position + _padding.TopLeft;
        }

        protected virtual void InternalUpdate(GameTimer gameTimer) { }

        public void Update(GameTimer gameTimer)
        {
            InternalUpdate(gameTimer);

            UIObject skip = null;

            if (this is UIScreen screen && screen.ExpandedDropdown != null)
            {
                if (!screen.CheckCloseExpandedDropdown())
                {
                    if (screen.ExpandedDropdown.IsActive)
                        screen.ExpandedDropdown.Update(gameTimer);

                    skip = screen.ExpandedDropdown;
                }
            }

            foreach (var child in Children)
            {
                if (child == skip)
                    continue;

                if (child.IsActive)
                    child.Update(gameTimer);
            }

            for (var i = UIAnimations.Count - 1; i >= 0; i--)
            {
                var animation = UIAnimations[i];
                animation.Update(gameTimer);

                if (animation.IsComplete)
                    UIAnimations.RemoveAt(i);
            }
        }

        internal virtual void PreDraw(SpriteBatch2D spriteBatch)
        {
            if (IgnoreOverflow)
                spriteBatch.PushScissorRect(0, null);
        }

        internal virtual void PostDraw(SpriteBatch2D spriteBatch)
        {
            if (IgnoreOverflow)
                spriteBatch.PopScissorRect(0);
        }

        protected virtual void InnerPreDraw(SpriteBatch2D spriteBatch) { }
        protected virtual void InnerPostDraw(SpriteBatch2D spriteBatch) { }
        protected virtual void InnerDraw(SpriteBatch2D spriteBatch) { }

        public void Draw(SpriteBatch2D spriteBatch)
        {
            if (!_isHidden)
                InnerDraw(spriteBatch);

            if (_useScissorRect)
                spriteBatch.PushScissorRect(0, PaddingBounds, true);

            InnerPreDraw(spriteBatch);

            UIObject skip = null;

            if (this is UIScreen screen && screen.ExpandedDropdown != null)
            {
                if (!screen.CheckCloseExpandedDropdown())
                    skip = screen.ExpandedDropdown;
            }

            foreach (var child in Children)
            {
                if (child == skip)
                    continue;

                DrawChild(child, spriteBatch);
            }

            if (skip != null)
                DrawChild(skip, spriteBatch);

            InnerPostDraw(spriteBatch);

            if (_useScissorRect)
                spriteBatch.PopScissorRect(0);
        }

        internal void DrawChild(UIObject child, SpriteBatch2D spriteBatch)
        {
            if (!child.IsVisible)
                return;
            if (_useScissorRect && !child.IgnoreOverflow && !Bounds.Intersects(child.Bounds))
                return;

            child.PreDraw(spriteBatch);
            child.Draw(spriteBatch);
            child.PostDraw(spriteBatch);
        }

        internal void ShowTooltip()
        {
            if (this is UIScreen)
                return;

            var parentScreen = ParentScreen;

            if (Tooltip.HasValue && parentScreen != null)
                parentScreen.ShowTooltip(this, Tooltip.Value);
        }

        internal void HideTooltip()
        {
            if (this is UIScreen)
                return;

            var parentScreen = ParentScreen;

            if (Tooltip.HasValue && parentScreen != null)
                parentScreen.HideTooltip(this);
        }

        #region Input Handling (Interface Passthrough)
        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);
        }

        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);
        }

        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            InternalHandleMouseButtonDown(mousePosition, button, gameTimer);
        }

        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);
        }

        public virtual void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
            InternalHandleKeyPressed(key, gameTimer);
        }

        public virtual void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            InternalHandleKeyReleased(key, gameTimer);
        }

        public virtual void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            InternalHandleKeyDown(key, gameTimer);
        }

        public virtual void HandleTextInput(char key, GameTimer gameTimer)
        {
            InternalHandleTextInput(key, gameTimer);
        }
        #endregion

        #region Input Handling
        internal UIObject GetFirstChildContainsMouse(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;

                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        internal UIObject GetFirstChildContainsMouseCanFocus(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;
                if (!child.CanFocus)
                    continue;

                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        internal UIObject GetFirstChildScrollableContainsMouse(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;

                if (!child._isScrollable)
                {
                    var hasScrollableChildren = child.GetFirstChildScrollableContainsMouse(mousePosition);
                    if (hasScrollableChildren == null)
                        continue;
                }

                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        internal UIObject GetFirstChildFocused()
        {
            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;
                if (!child.CanFocus)
                    return child;

                if (child.IsFocused)
                    return child;
            }

            return null;
        }

        internal virtual bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            var childCaptured = child?.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            foreach (var childNoMotion in Children)
            {
                if (childNoMotion == child)
                    continue;

                childNoMotion?.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
            }

            if (childCaptured.HasValue && childCaptured.Value == true)
            {
                return true;
            }
            else
            {
                if (!IsHovered)
                {
                    IsHovered = true;
                    OnHoverEnter?.Invoke(new OnHoverArgs(this));
                    ShowTooltip();
                }

                return false;
            }
        }

        internal virtual void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            if (IsHovered)
            {
                IsHovered = false;
                OnHoverExit?.Invoke(new OnHoverArgs(this));
                HideTooltip();
            }

            foreach (var childNoMotion in Children)
                childNoMotion?.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        internal virtual bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouseCanFocus(mousePosition);
            var childCaptured = child?.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;

            if (child == null && CanFocus)
                ParentScreen.FocusedObject = this;

            return false;
        }

        internal virtual bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            var childCaptured = child?.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;
            else
                return false;
        }

        internal virtual bool InternalHandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            var childCaptured = child?.InternalHandleMouseButtonDown(mousePosition, button, gameTimer);

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;
            else
                return false;
        }

        internal virtual bool InternalHandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var child = GetFirstChildScrollableContainsMouse(mousePosition);
            var childCaptured = child?.InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;

            if (child == null && _isScrollable)
            {
                if (mouseWheelDelta > 0)
                    ScrollUp();
                else if (mouseWheelDelta < 0)
                    ScrollDown();

                return true;
            }

            return false;
        }

        public virtual bool InternalHandleKeyPressed(Key key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleKeyPressed(key, gameTimer);

            return false;
        }

        public virtual bool InternalHandleKeyReleased(Key key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleKeyReleased(key, gameTimer);

            return false;
        }

        public virtual bool InternalHandleKeyDown(Key key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleKeyDown(key, gameTimer);

            return false;
        }

        public virtual bool InternalHandleTextInput(char key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleTextInput(key, gameTimer);

            return false;
        }
        #endregion

        public override string ToString()
        {
            return $"{GetType().Name} - {Name} [{Bounds}]";
        }
    }
}
