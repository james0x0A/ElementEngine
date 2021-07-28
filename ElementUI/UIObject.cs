﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

using Rectangle = ElementEngine.Rectangle;

namespace ElementEngine.ElementUI
{
    public class UIObject : IMouseHandler, IKeyboardHandler
    {
        public const int NO_DRAW_ORDER = -1;

        public UIObject Parent;
        public UIStyle Style => _style;
        public readonly List<UIObject> Children = new List<UIObject>();
        public readonly List<UIObject> ReverseChildren = new List<UIObject>();
        public string Name;
        public int DrawOrder = NO_DRAW_ORDER;

        #region Position, Size & Bounds
        public bool HasMargin => !_margins.IsZero;
        public bool HasPadding => !_padding.IsZero;

        public Vector2I Position
        {
            get => _position;
            set
            {
                _uiPosition.Position = value;
                _layoutDirty = true;
            }
        }

        public int X
        {
            get => _position.X;
            set
            {
                var current = _uiPosition.Position ?? Vector2I.Zero;
                current.X = value;
                _uiPosition.Position = current;
                _layoutDirty = true;
            }
        }

        public int Y
        {
            get => _position.Y;
            set
            {
                var current = _uiPosition.Position ?? Vector2I.Zero;
                current.Y = value;
                _uiPosition.Position = current;
                _layoutDirty = true;
            }
        }

        public Vector2I Size
        {
            get => _size;
            set
            {
                _uiSize.Size = value;
                _layoutDirty = true;
            }
        }

        public int Width
        {
            get => _size.X;
            set
            {
                var current = _uiSize.Size ?? Vector2I.Zero;
                current.X = value;
                _uiSize.Size = current;
                _layoutDirty = true;
            }
        }

        public int Height
        {
            get => _size.Y;
            set
            {
                var current = _uiSize.Size ?? Vector2I.Zero;
                current.Y = value;
                _uiSize.Size = current;
                _layoutDirty = true;
            }
        }

        public bool AutoWidth
        {
            get => _uiSize.AutoWidth;
            set
            {
                _uiSize.AutoWidth = value;
                _layoutDirty = true;
            }
        }

        public bool AutoHeight
        {
            get => _uiSize.AutoHeight;
            set
            {
                _uiSize.AutoHeight = value;
                _layoutDirty = true;
            }
        }

        public bool ParentWidth
        {
            get => _uiSize.ParentWidth;
            set
            {
                _uiSize.ParentWidth = value;
                _layoutDirty = true;
            }
        }

        public bool ParentHeight
        {
            get => _uiSize.ParentHeight;
            set
            {
                _uiSize.ParentHeight = value;
                _layoutDirty = true;
            }
        }

        public float? ParentWidthRatio
        {
            get => _uiSize.ParentWidthRatio;
            set
            {
                _uiSize.ParentWidthRatio = value;
                _layoutDirty = true;
            }
        }

        public float? ParentHeightRatio
        {
            get => _uiSize.ParentHeightRatio;
            set
            {
                _uiSize.ParentHeightRatio = value;
                _layoutDirty = true;
            }
        }

        public int? MinWidth
        {
            get => _uiSize.MinWidth;
            set
            {
                _uiSize.MinWidth = value;
                _layoutDirty = true;
            }
        }

        public int? MaxWidth
        {
            get => _uiSize.MaxWidth;
            set
            {
                _uiSize.MaxWidth = value;
                _layoutDirty = true;
            }
        }

        public int? MinHeight
        {
            get => _uiSize.MinHeight;
            set
            {
                _uiSize.MinHeight = value;
                _layoutDirty = true;
            }
        }

        public int? MaxHeight
        {
            get => _uiSize.MaxHeight;
            set
            {
                _uiSize.MaxHeight = value;
                _layoutDirty = true;
            }
        }

        public Rectangle Bounds
        {
            get => new Rectangle(_position, _size);
        }

        public Rectangle MarginBounds
        {
            get => new Rectangle(_position - _margins.TopLeft, _size + _margins.TopLeft + _margins.BottomRight);
        }

        public Rectangle PaddingBounds
        {
            get => new Rectangle(_position + _padding.TopLeft, _size - _padding.TopLeft - _padding.BottomRight);
        }
        #endregion

        #region Positioning

        public bool CenterX
        {
            get => _uiPosition.CenterX;
            set
            {
                _uiPosition.CenterX = value;
                _layoutDirty = true;
            }
        }

        public bool CenterY
        {
            get => _uiPosition.CenterX;
            set
            {
                _uiPosition.CenterX = value;
                _layoutDirty = true;
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
                _layoutDirty = true;
            }
        }

        public bool AnchorBottom
        {
            get => _uiPosition.AnchorBottom;
            set
            {
                _uiPosition.AnchorBottom = value;
                _layoutDirty = true;
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
                _layoutDirty = true;
            }
        }

        public bool AnchorRight
        {
            get => _uiPosition.AnchorRight;
            set
            {
                _uiPosition.AnchorRight = value;
                _layoutDirty = true;
            }
        }

        public void Center()
        {
            _uiPosition.CenterX = true;
            _uiPosition.CenterY = true;
            _layoutDirty = true;
        }
        #endregion

        #region Margins
        public int MarginLeft
        {
            get => _margins.Left;
            set
            {
                _margins.Left = value;
                _layoutDirty = true;
            }
        }

        public int MarginRight
        {
            get => _margins.Right;
            set
            {
                _margins.Right = value;
                _layoutDirty = true;
            }
        }

        public int MarginTop
        {
            get => _margins.Top;
            set
            {
                _margins.Top = value;
                _layoutDirty = true;
            }
        }

        public int MarginBottom
        {
            get => _margins.Bottom;
            set
            {
                _margins.Bottom = value;
                _layoutDirty = true;
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
            _layoutDirty = true;
        }
        #endregion

        #region Padding
        public int PaddingLeft
        {
            get => _padding.Left;
            set
            {
                _padding.Left = value;
                _layoutDirty = true;
            }
        }

        public int PaddingRight
        {
            get => _padding.Right;
            set
            {
                _padding.Right = value;
                _layoutDirty = true;
            }
        }

        public int PaddingTop
        {
            get => _padding.Top;
            set
            {
                _padding.Top = value;
                _layoutDirty = true;
            }
        }

        public int PaddingBottom
        {
            get => _padding.Bottom;
            set
            {
                _padding.Bottom = value;
                _layoutDirty = true;
            }
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
            _layoutDirty = true;
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

        public virtual void Show()
        {
            _isVisible = true;
        }

        public virtual void Hide()
        {
            _isVisible = false;
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
        }

        public void ToggleActive()
        {
            if (_isActive)
                Disable();
            else
                Enable();
        }
        #endregion

        internal bool _isActive = true;
        internal bool _isVisible = true;
        internal bool _useScissorRect => _style == null ? false : _style.OverflowType == OverflowType.Hide;

        internal UIStyle _style;
        internal UIPosition _uiPosition;
        internal UISize _uiSize;
        internal Vector2I _position;
        internal Vector2I _childOrigin;
        internal Vector2I _size;
        internal UISpacing _margins;
        internal UISpacing _padding;

        internal bool _layoutDirty = false;

        public UIObject(string name)
        {
            Name = name;
        }

        public void ApplyStyle(UIStyle style)
        {
            _style = style;
            _uiPosition = style.UIPosition ?? new UIPosition();
            _uiSize = style.UISize ?? new UISize();
            _margins = style.Margins ?? new UISpacing();
            _padding = style.Padding ?? new UISpacing();
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

            if (!_uiSize.IsAutoSizedX)
                Width = size.X;
            if (!_uiSize.IsAutoSizedY)
                Height = size.Y;
        }

        internal int GetHighestChildDrawOrder()
        {
            var val = NO_DRAW_ORDER;

            foreach (var child in Children)
                val = Math.Max(val, child.DrawOrder);

            return val;
        }

        public bool AddChild(UIObject child)
        {
            if (Children.AddIfNotContains(child))
            {
                if (child.DrawOrder == NO_DRAW_ORDER)
                {
                    if (Children.Count == 1)
                        child.DrawOrder = 1;
                    else
                        child.DrawOrder = GetHighestChildDrawOrder() + 1;
                }

                child.Parent = this;
                _layoutDirty = true;
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
                _layoutDirty = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveChild<T>() where T : UIObject
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                if (child is T)
                {
                    Children.RemoveAt(i);
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
                    Children.RemoveAt(i);
            }
        }

        internal virtual void CheckLayout()
        {
            if (_layoutDirty)
                UpdateLayout();

            foreach (var child in Children)
                child.CheckLayout();
        }

        internal virtual void UpdateLayout()
        {
            foreach (var child in Children)
                child.UpdateLayout();

            UpdateSize();
            UpdatePosition();

            foreach (var child in Children)
                child.UpdateLayout();

            HandleMargins();
            _layoutDirty = false;

            Children.Sort((c1, c2) => { return c1.DrawOrder.CompareTo(c2.DrawOrder); });
            ReverseChildren.Clear();
            ReverseChildren.AddRange(Children);
            ReverseChildren.Sort((c1, c2) => { return c2.DrawOrder.CompareTo(c1.DrawOrder); });
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

        internal virtual void HandleMargins()
        {
            UIObject firstChildWithMargin = null;

            foreach (var child in Children)
            {
                if (child.HasMargin)
                {
                    firstChildWithMargin = child;
                    break;
                }
            }

            // vertical margin
            foreach (var child in Children)
            {
                if (child == firstChildWithMargin)
                    continue;
                if (!child._uiPosition.Position.HasValue)
                    continue;
                if (child is UIContainer)
                    continue;
                if (!child.HasMargin)
                    continue;

                var sortedChildren = GlobalObjectPool<List<UIObject>>.Rent();
                sortedChildren.Clear();
                sortedChildren.AddRange(Children);
                sortedChildren.Sort((obj1, obj2) => { return obj1.Position.Y.CompareTo(obj2.Position.Y); });

                foreach (var sibling in sortedChildren)
                {
                    if (child == sibling)
                        continue;
                    if (!sibling.HasMargin)
                        continue;
                    if (sibling is UIContainer)
                        continue;
                    if (!child.MarginBounds.Intersects(sibling.MarginBounds))
                        continue;

                    if ((child.MarginTop > 0 || sibling.MarginBottom > 0) && !child._uiPosition.IsAutoPositionY)
                    {
                        if (child.MarginBounds.Top < sibling.MarginBounds.Bottom && child.MarginBounds.Bottom > sibling.MarginBounds.Top)
                        {
                            var offset = new Vector2I(0, sibling.MarginBounds.Bottom - child.MarginBounds.Top);
                            child._uiPosition.Position += offset;
                            child.UpdateLayout();
                        }
                    }
                }

                GlobalObjectPool<List<UIObject>>.Return(sortedChildren);
            }

            // horizontal margin
            foreach (var child in Children)
            {
                if (child == firstChildWithMargin)
                    continue;
                if (!child._uiPosition.Position.HasValue)
                    continue;
                if (child is UIContainer)
                    continue;
                if (!child.HasMargin)
                    continue;

                var sortedChildren = GlobalObjectPool<List<UIObject>>.Rent();
                sortedChildren.Clear();
                sortedChildren.AddRange(Children);
                sortedChildren.Sort((obj1, obj2) => { return obj1.Position.X.CompareTo(obj2.Position.X); });

                foreach (var sibling in sortedChildren)
                {
                    if (child == sibling)
                        continue;
                    if (!sibling.HasMargin)
                        continue;
                    if (sibling is UIContainer)
                        continue;
                    if (!child.MarginBounds.Intersects(sibling.MarginBounds))
                        continue;

                    if ((child.MarginLeft > 0 || sibling.MarginRight > 0) && !child._uiPosition.IsAutoPositionX)
                    {
                        if (child.MarginBounds.Left < sibling.MarginBounds.Right && child.MarginBounds.Right > sibling.MarginBounds.Left)
                        {
                            var offset = new Vector2I(sibling.MarginBounds.Right - child.MarginBounds.Left, 0);
                            child._uiPosition.Position += offset;
                            child.UpdateLayout();
                        }
                    }
                }

                GlobalObjectPool<List<UIObject>>.Return(sortedChildren);
            }
        } // HandleMargins

        public virtual void Update(GameTimer gameTimer)
        {
            foreach (var child in Children)
            {
                if (child.IsActive)
                    child.Update(gameTimer);
            }
        }

        public virtual void Draw(SpriteBatch2D spriteBatch)
        {
            if (_useScissorRect)
                spriteBatch.SetScissorRect(PaddingBounds, UIGlobals.SCISSOR_INDEX_OBJECT);

            foreach (var child in Children)
            {
                if (child.IsVisible)
                    child.Draw(spriteBatch);
            }

            if (_useScissorRect)
                spriteBatch.ResetScissorRect(UIGlobals.SCISSOR_INDEX_OBJECT);
        }

        #region Input Handling
        internal UIObject GetFirstChildContainsMouse(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        public virtual void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            child?.HandleMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        public virtual void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            child?.HandleMouseButtonPressed(mousePosition, button, gameTimer);
        }

        public virtual void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            child?.HandleMouseButtonReleased(mousePosition, button, gameTimer);
        }

        public virtual void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            child?.HandleMouseButtonDown(mousePosition, button, gameTimer);
        }

        public virtual void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            child?.HandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);
        }

        public virtual void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public virtual void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public virtual void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public virtual void HandleTextInput(char key, GameTimer gameTimer) { }
        #endregion

        public override string ToString()
        {
            return $"{GetType().Name} - {Name} [{Bounds}]";
        }

    } // UIObject
}
