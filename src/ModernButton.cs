using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Threading;

namespace RBWStack
{
    public class ModernButton : Button
    {
        private Color _normalColor = Color.FromArgb(255, 255, 255);
        private Color _hoverColor = Color.FromArgb(249, 250, 251);
        private Color _pressedColor = Color.FromArgb(243, 244, 246);
        private Color _borderColor = Color.FromArgb(229, 231, 235);
        private int _cornerRadius = 6;
        private string _iconGlyph = "";
        private bool _isSelected = false;
        private Color _selectedIndicatorColor = Color.FromArgb(16, 185, 129); // Emerald green

        private bool _isHovered = false;
        private bool _isPressed = false;

        public Color NormalColor
        {
            get { return _normalColor; }
            set { _normalColor = value; Invalidate(); }
        }

        public Color HoverColor
        {
            get { return _hoverColor; }
            set { _hoverColor = value; Invalidate(); }
        }

        public Color PressedColor
        {
            get { return _pressedColor; }
            set { _pressedColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        public string IconGlyph
        {
            get { return _iconGlyph; }
            set { _iconGlyph = value; Invalidate(); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; Invalidate(); }
        }

        public Color SelectedIndicatorColor
        {
            get { return _selectedIndicatorColor; }
            set { _selectedIndicatorColor = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.FromArgb(55, 65, 81); // Gray-700
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            Size = new Size(80, 28);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateBackColorFromParent();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            UpdateBackColorFromParent();
        }

        private void UpdateBackColorFromParent()
        {
            if (Parent != null)
            {
                Color parentColor = Parent.BackColor;
                Control p = Parent;
                while (parentColor == Color.Transparent && p.Parent != null)
                {
                    p = p.Parent;
                    parentColor = p.BackColor;
                }
                this.BackColor = parentColor;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            base.OnMouseUp(mevent);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Clear background with resolved parent backcolor
            using (SolidBrush parentBgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(parentBgBrush, ClientRectangle);
            }

            // Determine background color based on hover/press/selected
            Color currentBg = _normalColor;
            bool isHovered = _isHovered;
            bool isPressed = _isPressed;

            if (_isSelected)
            {
                currentBg = Color.FromArgb(243, 244, 246); // Subtle select Gray-100
            }
            else if (isPressed)
            {
                currentBg = _pressedColor;
            }
            else if (isHovered)
            {
                currentBg = _hoverColor;
            }

            if (!Enabled)
            {
                currentBg = Color.FromArgb(249, 250, 251);
            }

            // Fill rounded background
            using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, Width - 1, Height - 1), _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(currentBg))
                {
                    g.FillPath(brush, path);
                }

                // Draw border outline
                if (_borderColor != Color.Transparent && _borderColor.A > 0 && Enabled)
                {
                    using (Pen pen = new Pen(_borderColor, 1.0f))
                    {
                        pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Draw selection indicator stripe on the left edge for sidebar tabs
            if (_isSelected && _cornerRadius == 0)
            {
                using (SolidBrush indicatorBrush = new SolidBrush(_selectedIndicatorColor))
                {
                    g.FillRectangle(indicatorBrush, 0, 0, 4, Height);
                }
            }

            Color textCol = Enabled ? ForeColor : Color.FromArgb(156, 163, 175);

            // Draw icon and text
            if (!string.IsNullOrEmpty(_iconGlyph))
            {
                using (Font iconFont = new Font("Segoe MDL2 Assets", 9.5f))
                {
                    TextFormatFlags iconFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                    Rectangle iconRect = new Rectangle(14, 0, 20, Height);
                    TextRenderer.DrawText(g, _iconGlyph, iconFont, iconRect, textCol, iconFlags);
                }

                TextFormatFlags textFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                Rectangle textRect = new Rectangle(38, 0, Width - 40, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, textCol, textFlags);
            }
            else
            {
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                if (TextAlign == ContentAlignment.MiddleLeft)
                {
                    flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                    Rectangle paddedRect = new Rectangle(Padding.Left > 0 ? Padding.Left : 8, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical);
                    TextRenderer.DrawText(g, Text, Font, paddedRect, textCol, flags);
                }
                else
                {
                    TextRenderer.DrawText(g, Text, Font, ClientRectangle, textCol, flags);
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            if (radius == 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
