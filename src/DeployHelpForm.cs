using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace RBWStack
{
    public class DeployHelpForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private Panel _pnlHeader;
        private Label _btnClose;
        private RichTextBox _rtbContent;
        private ModernButton _btnCloseBottom;

        private Color colorBg = Color.FromArgb(248, 250, 252);
        private Color colorBorder = Color.FromArgb(226, 232, 240);
        private Color colorPurple = Color.FromArgb(139, 92, 246);
        private Color colorDark = Color.FromArgb(30, 41, 59);
        private Color colorDim = Color.FromArgb(100, 116, 139);
        private Color colorGreen = Color.FromArgb(22, 101, 52);
        private Color colorRed = Color.FromArgb(185, 28, 28);
        private Color colorOrange = Color.FromArgb(194, 65, 12);

        public DeployHelpForm()
        {
            this.Text = "Hướng dẫn Deploy Demo Hosting";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(680, 580);
            this.BackColor = colorBg;
            this.ShowInTaskbar = false;

            BuildUI();
            LoadGuideText();
        }

        private void BuildUI()
        {
            // Header
            _pnlHeader = new Panel();
            _pnlHeader.Location = new Point(0, 0);
            _pnlHeader.Size = new Size(this.Width, 44);
            _pnlHeader.BackColor = Color.White;
            _pnlHeader.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (Font font = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (SolidBrush br = new SolidBrush(colorPurple))
                    pe.Graphics.DrawString("HƯỚNG DẪN DEPLOY DEMO HOSTING", font, br, new PointF(16f, 12f));
                using (Pen pen = new Pen(colorBorder, 1f))
                    pe.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };
            _pnlHeader.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            this.Controls.Add(_pnlHeader);

            _btnClose = new Label();
            _btnClose.Text = "X";
            _btnClose.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _btnClose.ForeColor = colorDim;
            _btnClose.Location = new Point(this.Width - 38, 8);
            _btnClose.Size = new Size(28, 26);
            _btnClose.TextAlign = ContentAlignment.MiddleCenter;
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.Click += (s, e) => this.Close();
            _btnClose.MouseEnter += (s, e) => { _btnClose.ForeColor = Color.White; _btnClose.BackColor = colorRed; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.ForeColor = colorDim; _btnClose.BackColor = Color.Transparent; };
            _pnlHeader.Controls.Add(_btnClose);

            // Content container
            Panel pnlCard = new Panel();
            pnlCard.Location = new Point(14, 52);
            pnlCard.Size = new Size(this.Width - 28, this.Height - 110);
            pnlCard.BackColor = Color.White;
            pnlCard.Paint += (s, pe) => {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(colorBorder, 1f))
                    pe.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };
            this.Controls.Add(pnlCard);

            _rtbContent = new RichTextBox();
            _rtbContent.Location = new Point(12, 12);
            _rtbContent.Size = new Size(pnlCard.Width - 24, pnlCard.Height - 24);
            _rtbContent.BorderStyle = BorderStyle.None;
            _rtbContent.BackColor = Color.White;
            _rtbContent.ReadOnly = true;
            _rtbContent.Font = new Font("Segoe UI", 9.5f);
            _rtbContent.ScrollBars = RichTextBoxScrollBars.Vertical;
            pnlCard.Controls.Add(_rtbContent);

            // Bottom Footer
            Panel pnlFooter = new Panel();
            pnlFooter.Location = new Point(0, this.Height - 50);
            pnlFooter.Size = new Size(this.Width, 50);
            pnlFooter.BackColor = Color.White;
            pnlFooter.Paint += (s, pe) => {
                using (Pen pen = new Pen(colorBorder, 1f))
                    pe.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };
            this.Controls.Add(pnlFooter);

            _btnCloseBottom = new ModernButton();
            _btnCloseBottom.Text = "Đóng";
            _btnCloseBottom.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnCloseBottom.Location = new Point(this.Width - 110, 8);
            _btnCloseBottom.Size = new Size(94, 32);
            _btnCloseBottom.CornerRadius = 5;
            _btnCloseBottom.NormalColor = colorPurple;
            _btnCloseBottom.HoverColor = Color.FromArgb(124, 58, 237);
            _btnCloseBottom.ForeColor = Color.White;
            _btnCloseBottom.BorderColor = Color.Transparent;
            _btnCloseBottom.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(_btnCloseBottom);

            // Outer border
            this.Paint += (s, pe) => {
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                    pe.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
            };
        }

        private void AppendStyled(string text, Font font, Color color, int indent = 0)
        {
            _rtbContent.SelectionStart = _rtbContent.TextLength;
            _rtbContent.SelectionLength = 0;
            _rtbContent.SelectionFont = font;
            _rtbContent.SelectionColor = color;
            _rtbContent.SelectionIndent = indent;
            _rtbContent.AppendText(text);
        }

        private void LoadGuideText()
        {
            _rtbContent.Clear();

            Font fHeading = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Font fSubHeading = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Font fBody = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Font fBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            // 1. CÁCH CHẠY
            AppendStyled("1. CÁCH HOẠT ĐỘNG\n", fHeading, colorPurple);
            AppendStyled("Khi bấm Deploy, tool sẽ làm tự động các bước:\n", fBody, colorDark);
            AppendStyled("- Nén code web thành file dist.zip và xuất database thành dist.sql ở máy bạn.\n", fBody, colorDark, 12);
            AppendStyled("- Tải file bridge.php lên hosting qua FTP để làm cầu nối xử lý.\n", fBody, colorDark, 12);
            AppendStyled("- Gọi DirectAdmin tạo database mới (nếu chưa có).\n", fBody, colorDark, 12);
            AppendStyled("- Upload code + database lên, rồi gọi bridge giải nén và nạp data vào hosting.\n", fBody, colorDark, 12);
            AppendStyled("- Xong việc, tool sẽ tự xóa file bridge và file zip tạm trên hosting.\n\n", fBody, colorDark, 12);

            // 2. CẤU HÌNH ĐÚNG
            AppendStyled("2. CẤU HÌNH CHUẨN (TRONG TAB GENERAL)\n", fHeading, colorPurple);
            AppendStyled("Để không bị lỗi, các ô trong thẻ 'DEPLOY DEMO HOSTING' cài như sau:\n\n", fBody, colorDark);

            AppendStyled("- HOST / IP (FTP): ", fBold, colorDark, 12);
            AppendStyled("IP hosting của bạn (ví dụ: 210.2.64.70).\n", fBody, colorDark);

            AppendStyled("- FTP USER & PASSWORD: ", fBold, colorDark, 12);
            AppendStyled("Tài khoản DirectAdmin / FTP.\n", fBody, colorDark);

            AppendStyled("- FTP ROOT PATH: ", fBold, colorRed, 12);
            AppendStyled("LUÔN ĐỂ LÀ: ", fBold, colorDark);
            AppendStyled("/domains/{domain}/public_html\n", fBold, colorPurple);
            AppendStyled("  * Lưu ý: Phải để nguyên chữ {domain} như vậy. Tool sẽ tự lấy tên miền ở ô WEB DOMAIN điền vào. Sau này bạn có đổi sang demo46, demo46v2 hay v3 thì đường dẫn FTP vẫn luôn đúng, không bị lệch và không bao giờ bị lỗi 404.\n\n", fBody, colorGreen, 12);

            AppendStyled("- WEB DOMAIN: ", fBold, colorDark, 12);
            AppendStyled("Tên miền demo bạn muốn chạy (ví dụ: demo46v2.nasanivietnam.net). Không gõ http:// hay dấu gạch chéo cuối.\n", fBody, colorDark);

            AppendStyled("- DA USER: ", fBold, colorDark, 12);
            AppendStyled("Thường trùng với FTP USER.\n", fBody, colorDark);

            AppendStyled("- DA PORT: ", fBold, colorDark, 12);
            AppendStyled("Cổng DirectAdmin của server bạn (thường là 2222 hoặc 1111).\n\n", fBody, colorDark);

            // 3. CÁCH SỬA LỖI
            AppendStyled("3. CÁC LỖI THƯỜNG GẶP VÀ CÁCH SỬA\n", fHeading, colorPurple);

            AppendStyled("[!] Lỗi (404) Not Found khi kích hoạt bridge:\n", fSubHeading, colorRed);
            AppendStyled("- Nguyên nhân: Ô 'FTP ROOT PATH' đang gõ cứng tên domain khác với ô 'WEB DOMAIN' (ví dụ: file up vào thư mục demo46v2 nhưng web lại gọi demo46).\n", fBody, colorDark, 12);
            AppendStyled("- Cách sửa: Vào General, đổi lại ô FTP ROOT PATH thành /domains/{domain}/public_html là xong.\n\n", fBold, colorGreen, 12);

            AppendStyled("[!] Lỗi DirectAdmin API (Unauthorized / Access denied):\n", fSubHeading, colorRed);
            AppendStyled("- Sai pass FTP/DA hoặc sai DA PORT. Kiểm tra xem port 2222 hay 1111 và gõ lại đúng mật khẩu.\n\n", fBody, colorDark, 12);

            AppendStyled("[i] Cảnh báo: Không thể tải file .env từ hosting:\n", fSubHeading, colorOrange);
            AppendStyled("- Do dự án này mới up lần đầu nên trên host chưa có file .env cũ. Cứ để tool chạy tiếp bình thường, không sao cả.\n\n", fBody, colorDark, 12);

            AppendStyled("[?] Hộp thoại: 'Database đã có dữ liệu':\n", fSubHeading, Color.FromArgb(2, 132, 199));
            AppendStyled("- Chọn YES: Xoá data cũ trên host, nạp lại data từ máy lên.\n", fBody, colorDark, 12);
            AppendStyled("- Chọn NO: Giữ nguyên data trên host, chỉ update code web.\n", fBody, colorDark, 12);
            AppendStyled("- Chọn CANCEL: Huỷ bỏ, không làm tiếp.\n", fBody, colorDark, 12);

            _rtbContent.SelectionStart = 0;
            _rtbContent.ScrollToCaret();
        }
    }
}
