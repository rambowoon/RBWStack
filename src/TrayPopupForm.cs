using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace RBWStack
{
    public class TrayPopupForm : Form
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        private Action _onStartAll, _onStopAll, _onOpenUI, _onExit;
        private bool _isClosing = false;
        private Image _appLogo;

        public TrayPopupForm(
            string webServerName, bool isWebRunning,
            string phpVersion, bool isPhpRunning,
            bool isMysqlRunning,
            Action onStartAll, Action onStopAll,
            Action onOpenUI, Action onExit)
        {
            _onStartAll = onStartAll;
            _onStopAll = onStopAll;
            _onOpenUI = onOpenUI;
            _onExit = onExit;
            _appLogo = MainForm.LoadAppLogo();

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Width = 276;
            this.BackColor = Color.White;
            this.Padding = new Padding(0);

            // Tự đóng khi mất focus
            this.Deactivate += (s, e) => { if (!_isClosing) { _isClosing = true; this.Close(); } };

            int y = 0;

            // ── HEADER ──────────────────────────────
            Panel pnlHeader = new Panel();
            pnlHeader.Location = new Point(0, y);
            pnlHeader.Size = new Size(this.Width, 58);
            pnlHeader.BackColor = Color.White;
            pnlHeader.Paint += (s, pe) => {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // App Logo
                if (_appLogo != null)
                {
                    g.DrawImage(_appLogo, new Rectangle(14, 15, 26, 26));
                }
                else
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(238, 242, 255)))
                        g.FillEllipse(bg, 14, 15, 26, 26);
                    using (Font f = new Font("Segoe MDL2 Assets", 11f))
                    using (SolidBrush fg = new SolidBrush(Color.FromArgb(79, 70, 229)))
                        g.DrawString("\uE80F", f, fg, new PointF(18f, 18f));
                }

                // App Title
                using (Font fTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (SolidBrush brTitle = new SolidBrush(Color.FromArgb(15, 23, 42))) // Slate-900
                {
                    g.DrawString("RBW Stack", fTitle, brTitle, new PointF(48f, 12f));
                }

                // Subtitle
                using (Font fSub = new Font("Segoe UI", 8f, FontStyle.Regular))
                using (SolidBrush brSub = new SolidBrush(Color.FromArgb(100, 116, 139))) // Slate-500
                {
                    g.DrawString("Local Development Environment", fSub, brSub, new PointF(48f, 32f));
                }

                // Bottom divider line
                using (Pen p = new Pen(Color.FromArgb(241, 245, 249), 1f))
                {
                    g.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };
            this.Controls.Add(pnlHeader);
            y += 58;

            // ── LABEL "DỊCH VỤ" ────────────────────
            Panel pnlSvcHdr = new Panel();
            pnlSvcHdr.Location = new Point(0, y);
            pnlSvcHdr.Size = new Size(this.Width, 26);
            pnlSvcHdr.BackColor = Color.White;
            pnlSvcHdr.Paint += (s, pe) => {
                pe.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (Font f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(148, 163, 184))) // Slate-400
                {
                    pe.Graphics.DrawString("DỊCH VỤ HỆ THỐNG", f, c, new PointF(16f, 8f));
                }
            };
            this.Controls.Add(pnlSvcHdr);
            y += 26;

            // ── SERVICE ROWS ────────────────────────
            string webLabel = string.IsNullOrEmpty(webServerName) ? "Web Server" : webServerName;
            string phpLabel = string.IsNullOrEmpty(phpVersion) ? "PHP Engine" : ("PHP " + phpVersion);
            y = AddServiceRow(webLabel, isWebRunning, y, false);
            y = AddServiceRow(phpLabel, isPhpRunning, y, false);
            y = AddServiceRow("MySQL / MariaDB", isMysqlRunning, y, true);

            y += 4;
            AddDivider(y); y += 8;

            // ── ACTIONS ─────────────────────────────
            bool allRunning = isWebRunning && isPhpRunning && isMysqlRunning;
            bool anyRunning = isWebRunning || isPhpRunning || isMysqlRunning;

            if (!allRunning)
            {
                y = AddActionRow("\uE768", "Khởi động tất cả", Color.FromArgb(5, 150, 105), Color.FromArgb(236, 253, 245), y, _onStartAll, true);
            }

            if (anyRunning)
            {
                y = AddActionRow("\uE71A", "Dừng tất cả", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242), y, _onStopAll, false);
            }

            AddDivider(y); y += 8;

            y = AddActionRow("\uE737", "Mở giao diện chính", Color.FromArgb(30, 41, 59), Color.FromArgb(241, 245, 249), y, _onOpenUI, false);

            AddDivider(y); y += 8;

            y = AddActionRow("\uE7E8", "Thoát ứng dụng", Color.FromArgb(239, 68, 68), Color.FromArgb(254, 242, 242), y, _onExit, false);

            y += 8;
            this.Height = y;

            // Apply modern rounded region
            try
            {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, 14, 14));
            }
            catch { }
        }

        private int AddServiceRow(string name, bool running, int y, bool isLast)
        {
            Panel row = new Panel();
            row.Location = new Point(0, y);
            row.Size = new Size(this.Width, 36);
            row.BackColor = Color.White;

            row.Paint += (s, pe) => {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Status Dot
                Color dotColor = running ? Color.FromArgb(16, 185, 129) : Color.FromArgb(203, 213, 225);
                using (SolidBrush br = new SolidBrush(dotColor))
                {
                    g.FillEllipse(br, 16, 14, 8, 8);
                }

                // Service Name
                using (Font f = new Font("Segoe UI", 9.25f, FontStyle.Regular))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(30, 41, 59))) // Slate-800
                {
                    g.DrawString(name, f, c, new PointF(34f, 8f));
                }

                // Modern Pill Badge on the right
                string badgeText = running ? "Chạy" : "Dừng";
                Color badgeBg = running ? Color.FromArgb(236, 253, 245) : Color.FromArgb(248, 250, 252);
                Color badgeBorder = running ? Color.FromArgb(167, 243, 208) : Color.FromArgb(226, 232, 240);
                Color badgeTextCol = running ? Color.FromArgb(5, 150, 105) : Color.FromArgb(148, 163, 184);

                using (Font fBadge = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(badgeText, fBadge);
                    int badgeW = (int)sz.Width + 14;
                    int badgeH = 18;
                    int badgeX = this.Width - badgeW - 14;
                    int badgeY = 9;

                    Rectangle badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                    using (GraphicsPath path = GetRoundedPath(badgeRect, 4))
                    {
                        using (SolidBrush brBg = new SolidBrush(badgeBg))
                        {
                            g.FillPath(brBg, path);
                        }
                        using (Pen pBorder = new Pen(badgeBorder, 1f))
                        {
                            g.DrawPath(pBorder, path);
                        }
                    }

                    using (SolidBrush brText = new SolidBrush(badgeTextCol))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(badgeText, fBadge, brText, badgeRect, sf);
                    }
                }

                // Bottom divider
                if (!isLast)
                {
                    using (Pen p = new Pen(Color.FromArgb(248, 250, 252), 1f))
                    {
                        g.DrawLine(p, 34, 35, this.Width - 14, 35);
                    }
                }
            };

            this.Controls.Add(row);
            return y + 36;
        }

        private void AddDivider(int y)
        {
            Panel div = new Panel();
            div.Location = new Point(0, y);
            div.Size = new Size(this.Width, 6);
            div.BackColor = Color.White;
            div.Paint += (s, pe) => {
                using (Pen p = new Pen(Color.FromArgb(241, 245, 249), 1f))
                {
                    pe.Graphics.DrawLine(p, 14, 2, this.Width - 14, 2);
                }
            };
            this.Controls.Add(div);
        }

        private int AddActionRow(string glyph, string text, Color textColor, Color hoverBgColor, int y, Action onClick, bool isBold)
        {
            Panel row = new Panel();
            row.Location = new Point(6, y);
            row.Size = new Size(this.Width - 12, 34);
            row.BackColor = Color.White;
            row.Cursor = Cursors.Hand;

            bool isHovered = false;

            row.Paint += (s, pe) => {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                if (isHovered)
                {
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, row.Width - 1, row.Height - 1), 6))
                    using (SolidBrush br = new SolidBrush(hoverBgColor))
                    {
                        g.FillPath(br, path);
                    }
                }

                // Icon Glyph
                if (!string.IsNullOrEmpty(glyph))
                {
                    using (Font fIcon = new Font("Segoe MDL2 Assets", 10.5f, FontStyle.Regular))
                    using (SolidBrush brIcon = new SolidBrush(textColor))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(glyph, fIcon, brIcon, new RectangleF(10, 0, 18, row.Height), sf);
                    }
                }

                // Action Text
                FontStyle fs = isBold ? FontStyle.Bold : FontStyle.Regular;
                using (Font f = new Font("Segoe UI", 9.25f, fs))
                using (SolidBrush c = new SolidBrush(textColor))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    g.DrawString(text, f, c, new RectangleF(34, 0, row.Width - 36, row.Height), sf);
                }
            };

            row.MouseEnter += (s, e) => { isHovered = true; row.Invalidate(); };
            row.MouseLeave += (s, e) => { isHovered = false; row.Invalidate(); };
            row.Click += (s, e) => {
                _isClosing = true;
                this.Close();
                if (onClick != null) onClick();
            };

            this.Controls.Add(row);
            return y + 36;
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Drop shadow for modern popups
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        // Outer border line
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}
