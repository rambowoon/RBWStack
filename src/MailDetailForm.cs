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
    public class MailDetailForm : Form
    {
        private WebBrowser webBody;
        private Label lblSubject;
        private Label lblFrom;
        private Label lblTo;
        private Label lblDate;

        public MailDetailForm(CaughtEmail email)
        {
            this.Text = "Chi tiết Email - " + email.Subject;
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.BackColor = Color.FromArgb(248, 250, 252); // Slate 50

            // Header Panel
            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 110;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Padding = new Padding(20, 15, 20, 10);
            pnlHeader.Paint += (s, e) => {
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f)) // Slate 200
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };
            this.Controls.Add(pnlHeader);

            lblSubject = new Label();
            lblSubject.Text = email.Subject;
            lblSubject.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblSubject.ForeColor = Color.FromArgb(30, 41, 59);
            lblSubject.Location = new Point(20, 12);
            lblSubject.Size = new Size(740, 24);
            lblSubject.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblSubject);

            lblFrom = new Label();
            lblFrom.Text = "Từ: " + email.From;
            lblFrom.Font = new Font("Segoe UI", 9f);
            lblFrom.ForeColor = Color.FromArgb(100, 116, 139);
            lblFrom.Location = new Point(20, 40);
            lblFrom.Size = new Size(740, 18);
            lblFrom.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblFrom);

            lblTo = new Label();
            lblTo.Text = "Đến: " + email.To;
            lblTo.Font = new Font("Segoe UI", 9f);
            lblTo.ForeColor = Color.FromArgb(100, 116, 139);
            lblTo.Location = new Point(20, 58);
            lblTo.Size = new Size(740, 18);
            lblTo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblTo);

            lblDate = new Label();
            lblDate.Text = "Thời gian: " + email.Date;
            lblDate.Font = new Font("Segoe UI", 8.5f);
            lblDate.ForeColor = Color.FromArgb(100, 116, 139);
            lblDate.Location = new Point(20, 76);
            lblDate.Size = new Size(580, 18);
            lblDate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Controls.Add(lblDate);

            // Open in external browser button
            ModernButton btnOpenBrowser = new ModernButton();
            btnOpenBrowser.Text = "Mở Browser ↗";
            btnOpenBrowser.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnOpenBrowser.Size = new Size(110, 30);
            btnOpenBrowser.Location = new Point(650, 15);
            btnOpenBrowser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenBrowser.NormalColor = Color.FromArgb(241, 245, 249);
            btnOpenBrowser.HoverColor = Color.FromArgb(226, 232, 240);
            btnOpenBrowser.BorderColor = Color.FromArgb(203, 213, 225);
            btnOpenBrowser.ForeColor = Color.FromArgb(30, 41, 59);
            btnOpenBrowser.Click += (s, e) => {
                MainForm.OpenEmailInExternalBrowser(email);
            };
            pnlHeader.Controls.Add(btnOpenBrowser);

            // Web Browser Container
            Panel pnlBrowserWrap = new Panel();
            pnlBrowserWrap.Dock = DockStyle.Fill;
            pnlBrowserWrap.Padding = new Padding(20);
            pnlBrowserWrap.BackColor = Color.FromArgb(248, 250, 252);
            this.Controls.Add(pnlBrowserWrap);
            pnlBrowserWrap.BringToFront();

            webBody = new WebBrowser();
            webBody.Dock = DockStyle.Fill;
            webBody.ScriptErrorsSuppressed = true;
            webBody.Navigating += (s, e) => {
                string urlObj = e.Url.ToString();
                if (urlObj != "about:blank")
                {
                    e.Cancel = true;
                    if (urlObj.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        urlObj.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        try { System.Diagnostics.Process.Start(urlObj); } catch { }
                    }
                }
            };
            pnlBrowserWrap.Controls.Add(webBody);

            string initialHtml = email.IsHtml ? email.Body : "<html><body><pre style='font-family: Consolas, monospace; font-size: 12px; white-space: pre-wrap;'>" + SimpleHtmlEncode(email.Body) + "</pre></body></html>";
            webBody.DocumentText = initialHtml;

            // Resolve relative paths in background task to avoid UI freeze
            System.Threading.Tasks.Task.Run(() => {
                try
                {
                    string processedHtml = MainForm.ResolveHtmlImageSources(initialHtml);
                    if (processedHtml != initialHtml && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action(() => {
                            if (!this.IsDisposed && webBody != null)
                            {
                                webBody.DocumentText = processedHtml;
                            }
                        }));
                    }
                }
                catch { }
            });
        }

        private static string SimpleHtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }
    }
}
