using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
    public class VirtualHostForm : Form
    {
        private string _sitePath;
        private string _folderName;

        private TextBox _txtDomain;
        private ModernButton _btnSave;
        private ModernButton _btnCancel;

        private Color colorBg = Color.White;
        private Color colorBorder = Color.FromArgb(226, 232, 240);
        private Color colorText = Color.FromArgb(15, 23, 42);
        private Color colorTextDim = Color.FromArgb(100, 116, 139);


        private void ApplyRoundedRegion(Control control, int radius)
        {
            // Fully disabled to prevent Access Violations / GDI+ Region crashes
        }

        public VirtualHostForm(string sitePath, string folderName)
        {
            _sitePath = sitePath;
            _folderName = folderName;

            this.Text = "Cấu hình Host ảo - " + folderName;
            this.Size = new Size(380, 180);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(248, 250, 252); // Slate 50 background
            this.ShowInTaskbar = false;

            // Paint Outer Border (matching Deploy form)
            this.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen penPopup = new Pen(Color.FromArgb(148, 163, 184), 1.5f)) // Slate 400
                {
                    pe.Graphics.DrawRectangle(penPopup, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // ── HEADER PANEL (matching Deploy form) ──────────────────────────
            Panel pnlHeader = new Panel();
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Size = new Size(380, 40);
            pnlHeader.BackColor = Color.White;
            pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (Font font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (SolidBrush brIcon = new SolidBrush(Color.FromArgb(37, 99, 235)))
                using (SolidBrush brText = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    pe.Graphics.DrawString("🌐", font, brIcon, new PointF(14f, 10f));
                    pe.Graphics.DrawString("CẤU HÌNH HOST ẢO LOCAL", font, brText, new PointF(34f, 10f));
                }
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    pe.Graphics.DrawLine(pen, 0, 39, 380, 39);
                }
            };
            pnlHeader.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    MainForm.ReleaseCapture();
                    MainForm.SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            this.Controls.Add(pnlHeader);

            Label btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnClose.ForeColor = Color.FromArgb(148, 163, 184); // Slate 400
            btnClose.Location = new Point(345, 8);
            btnClose.Size = new Size(26, 24);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.FromArgb(239, 68, 68); };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(148, 163, 184); btnClose.BackColor = Color.Transparent; };
            pnlHeader.Controls.Add(btnClose);

            // ── DOMAIN FIELD ──────────────────────────────────────────
            bool enabled;
            string domain;
            bool useSsl;
            MainForm.GetVHostConfig(sitePath, folderName, out enabled, out domain, out useSsl);

            Label lblDomain = new Label();
            lblDomain.Text = "Tên miền ảo:";
            lblDomain.Font = new Font("Segoe UI", 9f);
            lblDomain.ForeColor = Color.FromArgb(100, 116, 139); // Slate 500
            lblDomain.Location = new Point(20, 68);
            lblDomain.Size = new Size(90, 20);
            this.Controls.Add(lblDomain);

            Panel pnlDomainWrap = new Panel();
            pnlDomainWrap.Location = new Point(115, 62);
            pnlDomainWrap.Size = new Size(245, 30);
            pnlDomainWrap.BackColor = Color.White;
            pnlDomainWrap.Paint += (s, paintEvt) => {
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f)) // Slate 300
                {
                    paintEvt.Graphics.DrawRectangle(pen, 0, 0, pnlDomainWrap.Width - 1, pnlDomainWrap.Height - 1);
                }
            };
            this.Controls.Add(pnlDomainWrap);
            ApplyRoundedRegion(pnlDomainWrap, 6);

            _txtDomain = new TextBox();
            _txtDomain.Text = domain;
            _txtDomain.Font = new Font("Segoe UI", 9.5f);
            _txtDomain.Location = new Point(8, 6);
            _txtDomain.Size = new Size(229, 18);
            _txtDomain.BorderStyle = BorderStyle.None;
            _txtDomain.BackColor = Color.White;
            _txtDomain.ForeColor = Color.FromArgb(15, 23, 42); // Slate 900
            pnlDomainWrap.Controls.Add(_txtDomain);

            // ── ACTION BUTTONS ─────────────────────────────────────────
            if (!enabled)
            {
                // Host ảo chưa bật: hiện nút "Bật Host ảo" và "Hủy"
                _btnSave = new ModernButton();
                _btnSave.Text = "Bật Host ảo";
                _btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                _btnSave.NormalColor = Color.FromArgb(34, 197, 94); // Green-500
                _btnSave.HoverColor = Color.FromArgb(22, 163, 74); // Green-600
                _btnSave.ForeColor = Color.White;
                _btnSave.Location = new Point(70, 118);
                _btnSave.Size = new Size(130, 32);
                _btnSave.CornerRadius = 6;
                _btnSave.Click += (s, e) => {
                    string newDomain = _txtDomain.Text.Trim().ToLower();
                    if (string.IsNullOrEmpty(newDomain))
                    {
                        MessageBox.Show("Vui lòng nhập tên miền ảo hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    MainForm.SaveVHostConfig(_sitePath, true, newDomain, true);
                    MainForm.UpdateAllProjectsEnvFiles("");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(_btnSave);

                _btnCancel = new ModernButton();
                _btnCancel.Text = "Hủy";
                _btnCancel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                _btnCancel.NormalColor = Color.White;
                _btnCancel.HoverColor = Color.FromArgb(243, 244, 246);
                _btnCancel.ForeColor = Color.FromArgb(107, 114, 128);
                _btnCancel.BorderColor = Color.FromArgb(229, 231, 235);
                _btnCancel.Location = new Point(210, 118);
                _btnCancel.Size = new Size(100, 32);
                _btnCancel.CornerRadius = 6;
                _btnCancel.Click += (s, e) => {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                this.Controls.Add(_btnCancel);
            }
            else
            {
                // Host ảo đang chạy: hiện nút "Cập nhật", "Tắt Host ảo" và "Hủy"
                _btnSave = new ModernButton();
                _btnSave.Text = "Cập nhật";
                _btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                _btnSave.NormalColor = Color.FromArgb(59, 130, 246); // Blue-500
                _btnSave.HoverColor = Color.FromArgb(37, 99, 235); // Blue-600
                _btnSave.ForeColor = Color.White;
                _btnSave.Location = new Point(30, 118);
                _btnSave.Size = new Size(100, 32);
                _btnSave.CornerRadius = 6;
                _btnSave.Click += (s, e) => {
                    string newDomain = _txtDomain.Text.Trim().ToLower();
                    if (string.IsNullOrEmpty(newDomain))
                    {
                        MessageBox.Show("Vui lòng nhập tên miền ảo hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (newDomain != domain)
                    {
                        MainForm.RemoveHostsEntry(domain);
                    }

                    MainForm.SaveVHostConfig(_sitePath, true, newDomain, true);
                    MainForm.UpdateAllProjectsEnvFiles("");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(_btnSave);

                ModernButton btnToggleOff = new ModernButton();
                btnToggleOff.Text = "Tắt Host ảo";
                btnToggleOff.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnToggleOff.NormalColor = Color.FromArgb(239, 68, 68); // Red-500
                btnToggleOff.HoverColor = Color.FromArgb(220, 38, 38); // Red-600
                btnToggleOff.ForeColor = Color.White;
                btnToggleOff.Location = new Point(140, 118);
                btnToggleOff.Size = new Size(110, 32);
                btnToggleOff.CornerRadius = 6;
                btnToggleOff.Click += (s, e) => {
                    MainForm.RemoveHostsEntry(domain);
                    MainForm.SaveVHostConfig(_sitePath, false, domain, true);
                    MainForm.UpdateAllProjectsEnvFiles("");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnToggleOff);

                _btnCancel = new ModernButton();
                _btnCancel.Text = "Hủy";
                _btnCancel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                _btnCancel.NormalColor = Color.White;
                _btnCancel.HoverColor = Color.FromArgb(243, 244, 246);
                _btnCancel.ForeColor = Color.FromArgb(107, 114, 128);
                _btnCancel.BorderColor = Color.FromArgb(229, 231, 235);
                _btnCancel.Location = new Point(260, 118);
                _btnCancel.Size = new Size(80, 32);
                _btnCancel.CornerRadius = 6;
                _btnCancel.Click += (s, e) => {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                this.Controls.Add(_btnCancel);
            }
        }
    }
}
