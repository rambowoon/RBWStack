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
            set { _cornerRadius = value; UpdateRegion(); Invalidate(); }
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
            FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            ForeColor = Color.FromArgb(51, 65, 85); // Slate-700
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            Size = new Size(80, 28);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (_cornerRadius > 0 && Width > 0 && Height > 0)
            {
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, Width, Height), _cornerRadius))
                {
                    this.Region = new Region(path);
                }
            }
            else
            {
                this.Region = null;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Fully suppress base button background rendering to avoid border/corner artifacts
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
            Color parentBg = Color.White;
            Control parentCtrl = Parent;
            while (parentCtrl != null)
            {
                if (parentCtrl.BackColor != Color.Transparent && parentCtrl.BackColor.A == 255)
                {
                    parentBg = parentCtrl.BackColor;
                    break;
                }
                parentCtrl = parentCtrl.Parent;
            }
            using (SolidBrush parentBgBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(parentBgBrush, ClientRectangle);
            }

            // Determine background color based on hover/press/selected
            Color currentBg = _normalColor;
            bool isHovered = _isHovered;
            bool isPressed = _isPressed;

            if (_isSelected)
            {
                currentBg = Color.FromArgb(236, 253, 245); // Soft Emerald-50 tint
            }
            else if (isPressed)
            {
                currentBg = _pressedColor;
            }
            else if (isHovered)
            {
                currentBg = Color.FromArgb(241, 245, 249); // Soft Slate-100 hover
            }

            if (!Enabled)
            {
                currentBg = Color.FromArgb(249, 250, 251);
            }

            // Fill background & draw border
            if (_cornerRadius == 0)
            {
                using (SolidBrush brush = new SolidBrush(currentBg))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
                if (_borderColor != Color.Transparent && _borderColor.A > 0 && Enabled)
                {
                    using (Pen pen = new Pen(_borderColor, 1.0f))
                    {
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
            else
            {
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
                            g.DrawPath(pen, path);
                        }
                    }
                }
            }

            // Draw selection indicator stripe on the left edge for sidebar tabs
            if (_isSelected && _cornerRadius == 0)
            {
                using (SolidBrush indicatorBrush = new SolidBrush(_selectedIndicatorColor))
                {
                    g.FillRectangle(indicatorBrush, 0, 4, 3, Height - 8);
                }
            }
            else if (_isHovered && !_isSelected && _cornerRadius == 0)
            {
                using (SolidBrush hoverBarBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    g.FillRectangle(hoverBarBrush, 0, 8, 2, Height - 16);
                }
            }

            Color textCol = Enabled 
                ? (_isSelected ? Color.FromArgb(6, 95, 70) 
                   : (_isHovered ? Color.FromArgb(15, 23, 42) 
                   : ForeColor)) 
                : Color.FromArgb(156, 163, 175);

            Font textDrawFont = Font;
            bool createdFont = false;
            if (_isSelected && Font.Style != FontStyle.Bold)
            {
                try { textDrawFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold); createdFont = true; } catch { }
            }

            try
            {
                // Draw icon and text
                if (!string.IsNullOrEmpty(_iconGlyph))
                {
                    Color iconCol = Enabled 
                        ? (_isSelected ? Color.FromArgb(16, 185, 129) 
                           : (_isHovered ? Color.FromArgb(14, 165, 233) 
                           : Color.FromArgb(100, 116, 139))) 
                        : Color.FromArgb(160, 170, 185);

                    var oldHint = g.TextRenderingHint;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    using (Font iconFont = new Font("Segoe MDL2 Assets", 11.5f, FontStyle.Regular))
                    using (SolidBrush iconBrush = new SolidBrush(iconCol))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        RectangleF iconRect = new RectangleF(12f, 0, 20, Height);
                        g.DrawString(_iconGlyph, iconFont, iconBrush, iconRect, sf);
                    }
                    g.TextRenderingHint = oldHint;

                    TextFormatFlags textFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                    Rectangle textRect = new Rectangle(38, 0, Width - 40, Height);
                    TextRenderer.DrawText(g, Text, textDrawFont, textRect, textCol, textFlags);
                }
                else
                {
                    TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                    if (TextAlign == ContentAlignment.MiddleLeft)
                    {
                        flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                        Rectangle paddedRect = new Rectangle(Padding.Left > 0 ? Padding.Left : 8, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical);
                        TextRenderer.DrawText(g, Text, textDrawFont, paddedRect, textCol, flags);
                    }
                    else
                    {
                        TextRenderer.DrawText(g, Text, textDrawFont, ClientRectangle, textCol, flags);
                    }
                }
            }
            finally
            {
                if (createdFont && textDrawFont != null) textDrawFont.Dispose();
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

    public class SmoothScrollHelper
    {
        private readonly Panel _panel;
        private readonly System.Windows.Forms.Timer _timer;
        private int _targetY = 0;
        private int _currentY = 0;

        public SmoothScrollHelper(Panel panel)
        {
            _panel = panel;
            _panel.MouseWheel += HandleMouseWheel;
            _timer = new System.Windows.Forms.Timer { Interval = 15 };
            _timer.Tick += Timer_Tick;
        }

        public void Attach(Control control)
        {
            if (control == null) return;
            control.MouseWheel -= HandleMouseWheel;
            control.MouseWheel += HandleMouseWheel;
            foreach (Control child in control.Controls)
            {
                Attach(child);
            }
        }

        private void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            HandledMouseEventArgs hme = e as HandledMouseEventArgs;
            if (hme != null) hme.Handled = true;

            int max = _panel.VerticalScroll.Maximum - _panel.ClientRectangle.Height;
            if (max <= 0) return;

            if (!_timer.Enabled)
            {
                _currentY = -_panel.AutoScrollPosition.Y;
                _targetY = _currentY;
            }

            int step = -(e.Delta / 120) * 65;
            _targetY = Math.Max(0, Math.Min(max, _targetY + step));
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            int diff = _targetY - _currentY;
            if (Math.Abs(diff) <= 2)
            {
                _currentY = _targetY;
                _panel.AutoScrollPosition = new Point(0, _currentY);
                _timer.Stop();
            }
            else
            {
                _currentY += (int)Math.Round(diff * 0.32);
                _panel.AutoScrollPosition = new Point(0, _currentY);
            }
        }
    }
}
