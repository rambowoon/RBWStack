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
    public class TrayPopupForm : Form
    {
        private Action _onStartAll, _onStopAll, _onOpenUI, _onExit;
        private bool _isClosing = false;

        public TrayPopupForm(
            string webServerName, bool isWebRunning,
            string phpVersion,   bool isPhpRunning,
            bool isMysqlRunning,
            Action onStartAll, Action onStopAll,
            Action onOpenUI,   Action onExit)
        {
            _onStartAll = onStartAll;
            _onStopAll  = onStopAll;
            _onOpenUI   = onOpenUI;
            _onExit     = onExit;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar   = false;
            this.TopMost         = true;
            this.Width           = 268;
            this.BackColor       = Color.White;
            this.Padding         = new Padding(0);

            // T\u1ef1 \u0111\u00f3ng khi m\u1ea5t focus
            this.Deactivate += (s, e) => { if (!_isClosing) { _isClosing = true; this.Close(); } };

            int y = 0;

            // ── HEADER ──────────────────────────────
            Panel pnlHeader = new Panel();
            pnlHeader.Location  = new Point(0, y);
            pnlHeader.Size      = new Size(268, 54);
            pnlHeader.BackColor = Color.White;
            pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Icon nh\u1ecf ch\u1ea1m tr\u00f2n m\u00e0u indigo
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(238, 242, 255)))
                    pe.Graphics.FillEllipse(bg, 14, 15, 26, 26);
                using (Font f = new Font("Segoe MDL2 Assets", 10f))
                using (SolidBrush fg = new SolidBrush(Color.FromArgb(79, 70, 229)))
                    pe.Graphics.DrawString("\uEC27", f, fg, new PointF(17f, 18f));
                // T\u00ean app
                using (Font f = new Font("Segoe UI Semibold", 10f, FontStyle.Bold))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(17, 24, 39)))
                    pe.Graphics.DrawString("RBW Stack", f, c, new PointF(48f, 18f));
                // \u0110\u01b0\u1eddng k\u1ebb d\u01b0\u1edbi
                using (Pen p = new Pen(Color.FromArgb(243, 244, 246)))
                    pe.Graphics.DrawLine(p, 0, 53, 267, 53);
            };
            this.Controls.Add(pnlHeader);
            y += 54;

            // ── LABEL "DỊCH VỤ" ────────────────────
            Panel pnlSvcHdr = new Panel();
            pnlSvcHdr.Location  = new Point(0, y);
            pnlSvcHdr.Size      = new Size(268, 28);
            pnlSvcHdr.BackColor = Color.White;
            pnlSvcHdr.Paint += (s, pe) => {
                using (Font f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(156, 163, 175)))
                    pe.Graphics.DrawString("D\u1ecaCH V\u1ee4", f, c, new PointF(16f, 8f));
            };
            this.Controls.Add(pnlSvcHdr);
            y += 28;

            // ── SERVICE ROWS ────────────────────────
            string webLabel = webServerName;
            string phpLabel = string.IsNullOrEmpty(phpVersion) ? "PHP-CGI" : ("PHP  " + phpVersion);
            y = AddServiceRow(webLabel,         isWebRunning,   y, false);
            y = AddServiceRow(phpLabel,         isPhpRunning,   y, false);
            y = AddServiceRow("MySQL / MariaDB", isMysqlRunning, y, true);

            y += 2;
            AddDivider(y); y += 10;

            // ── ACTIONS ─────────────────────────────
            bool allRunning = isWebRunning && isPhpRunning && isMysqlRunning;
            bool anyRunning = isWebRunning || isPhpRunning || isMysqlRunning;

            // Chỉ hiện "Khởi động" khi chưa start hết
            if (!allRunning)
                y = AddActionRow("\u25b6  Kh\u1edfi \u0111\u1ed9ng t\u1ea5t c\u1ea3", Color.FromArgb(16, 185, 129), y, _onStartAll, true);

            // Chỉ hiện "Dừng" khi có ít nhất 1 service đang chạy
            if (anyRunning)
                y = AddActionRow("\u25a0  D\u1eebng t\u1ea5t c\u1ea3", Color.FromArgb(239, 68, 68), y, _onStopAll, false);

            AddDivider(y); y += 10;

            y = AddActionRow("M\u1edf giao di\u1ec7n", Color.FromArgb(31, 41, 55), y, _onOpenUI, false);

            AddDivider(y); y += 10;

            y = AddActionRow("Tho\u00e1t \u1ee9ng d\u1ee5ng", Color.FromArgb(239, 68, 68), y, _onExit, false);

            y += 6;
            this.Height = y;
        }

        private int AddServiceRow(string name, bool running, int y, bool isLast)
        {
            Panel row = new Panel();
            row.Location  = new Point(0, y);
            row.Size      = new Size(268, 38);
            row.BackColor = Color.White;
            bool capturedRunning = running;
            bool capturedIsLast  = isLast;

            row.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Status dot
                Color dotColor = capturedRunning
                    ? Color.FromArgb(16, 185, 129)
                    : Color.FromArgb(209, 213, 219);
                using (SolidBrush br = new SolidBrush(dotColor))
                    pe.Graphics.FillEllipse(br, 16, 15, 8, 8);

                // Service name
                using (Font f = new Font("Segoe UI", 9.5f))
                using (SolidBrush c = new SolidBrush(Color.FromArgb(31, 41, 55)))
                    pe.Graphics.DrawString(name, f, c, new PointF(34f, 11f));

                // Status badge (right side)
                string badge   = capturedRunning ? "Ch\u1ea1y" : "D\u1eebng";
                Color badgeCol = capturedRunning
                    ? Color.FromArgb(16, 185, 129)
                    : Color.FromArgb(156, 163, 175);
                using (Font f = new Font("Segoe UI", 8f))
                using (SolidBrush c = new SolidBrush(badgeCol))
                {
                    SizeF sz = pe.Graphics.MeasureString(badge, f);
                    pe.Graphics.DrawString(badge, f, c, new PointF(268f - sz.Width - 14f, 13f));
                }

                // Bottom rule
                if (!capturedIsLast)
                    using (Pen p = new Pen(Color.FromArgb(249, 250, 251)))
                        pe.Graphics.DrawLine(p, 32, 37, 252, 37);
            };
            this.Controls.Add(row);
            return y + 38;
        }

        private void AddDivider(int y)
        {
            Panel div = new Panel();
            div.Location  = new Point(0, y);
            div.Size      = new Size(268, 10);
            div.BackColor = Color.White;
            div.Paint += (s, pe) => {
                using (Pen p = new Pen(Color.FromArgb(243, 244, 246)))
                    pe.Graphics.DrawLine(p, 14, 5, 254, 5);
            };
            this.Controls.Add(div);
        }

        private int AddActionRow(string text, Color textColor, int y, Action onClick, bool isBold)
        {
            Panel row = new Panel();
            row.Location  = new Point(0, y);
            row.Size      = new Size(268, 36);
            row.BackColor = Color.White;
            row.Cursor    = Cursors.Hand;

            Color capturedColor = textColor;
            string capturedText = text;
            bool capturedBold   = isBold;
            Action capturedClick = onClick;

            row.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                FontStyle fs = capturedBold ? FontStyle.Bold : FontStyle.Regular;
                using (Font f = new Font("Segoe UI Semibold", 9.5f, fs))
                using (SolidBrush c = new SolidBrush(capturedColor))
                    pe.Graphics.DrawString(capturedText, f, c, new PointF(16f, 10f));
            };

            row.MouseEnter += (s, e) => { row.BackColor = Color.FromArgb(249, 250, 251); row.Invalidate(); };
            row.MouseLeave += (s, e) => { row.BackColor = Color.White; row.Invalidate(); };
            row.Click += (s, e) => {
                _isClosing = true;
                this.Close();
                if (capturedClick != null) capturedClick();
            };

            this.Controls.Add(row);
            return y + 36;
        }

        // Drop shadow
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        // Vi\u1ec1n ngo\u00e0i 1px m\u00e0u xanh nh\u1ea1t
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
        }
    }
}
