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
    public class ConfigEditorForm : Form
    {
        private string _filePath;
        private TextBox txtContent;
        private Label lblTitle;
        private ModernButton btnSave;
        private ModernButton btnCancel;
        private Panel pnlHeader;
        private Label btnClose;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public ConfigEditorForm(string title, string filePath)
        {
            _filePath = filePath;
            
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(700, 500);
            this.BackColor = Color.FromArgb(24, 24, 28);

            // Header panel
            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 40;
            pnlHeader.BackColor = Color.FromArgb(32, 32, 38);
            pnlHeader.MouseDown += Header_MouseDown;

            lblTitle = new Label();
            lblTitle.Text = "Config Editor - " + title;
            lblTitle.ForeColor = Color.FromArgb(240, 240, 245);
            lblTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 10);
            pnlHeader.Controls.Add(lblTitle);

            btnClose = new Label();
            btnClose.Text = "X";
            btnClose.ForeColor = Color.FromArgb(150, 150, 160);
            btnClose.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Size = new Size(30, 30);
            btnClose.Location = new Point(665, 8);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 150, 160);
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            Panel pnlEditor = new Panel();
            pnlEditor.Dock = DockStyle.Fill;
            pnlEditor.Padding = new Padding(15, 55, 15, 65);
            this.Controls.Add(pnlEditor);

            txtContent = new TextBox();
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Both;
            txtContent.Dock = DockStyle.Fill;
            txtContent.BackColor = Color.FromArgb(32, 32, 38);
            txtContent.ForeColor = Color.FromArgb(220, 220, 225);
            txtContent.Font = new Font("Consolas", 10f);
            txtContent.BorderStyle = BorderStyle.None;
            txtContent.WordWrap = false;
            pnlEditor.Controls.Add(txtContent);

            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 55;
            pnlBottom.BackColor = Color.FromArgb(24, 24, 28);
            
            btnSave = new ModernButton();
            btnSave.Text = "Lưu (Save)";
            btnSave.NormalColor = Color.FromArgb(46, 204, 113);
            btnSave.HoverColor = Color.FromArgb(39, 174, 96);
            btnSave.PressedColor = Color.FromArgb(30, 132, 73);
            btnSave.Location = new Point(475, 10);
            btnSave.Click += Save_Click;
            pnlBottom.Controls.Add(btnSave);

            btnCancel = new ModernButton();
            btnCancel.Text = "Hủy (Cancel)";
            btnCancel.NormalColor = Color.FromArgb(120, 120, 130);
            btnCancel.HoverColor = Color.FromArgb(100, 100, 110);
            btnCancel.PressedColor = Color.FromArgb(80, 80, 90);
            btnCancel.Location = new Point(585, 10);
            btnCancel.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnCancel);

            this.Controls.Add(pnlBottom);
            pnlBottom.BringToFront();

            LoadConfigContent();
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void LoadConfigContent()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    txtContent.Text = File.ReadAllText(_filePath);
                }
                else
                {
                    txtContent.Text = "; File config không tồn tại ở đường dẫn:\r\n; " + _filePath;
                    btnSave.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(_filePath, txtContent.Text);
                MessageBox.Show("Đã lưu cấu hình thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ghi file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
