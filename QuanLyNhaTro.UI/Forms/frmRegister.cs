using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System.Drawing.Drawing2D;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class frmRegister : Form
    {
        private readonly AuthService _authService = new();

        // Colors - Material Design
        private readonly Color PrimaryBlue = Color.FromArgb(30, 136, 229);      // #1E88E5
        private readonly Color PrimaryBlueDark = Color.FromArgb(25, 118, 210);  // #1976D2
        private readonly Color SuccessGreen = Color.FromArgb(46, 204, 113);     // #2ECC71 or use Blue
        private readonly Color HoverGreen = Color.FromArgb(39, 174, 96);        // #27AE60
        private readonly Color BorderColor = Color.FromArgb(208, 215, 226);     // #D0D7E2
        private readonly Color TextDark = Color.FromArgb(30, 41, 59);
        private readonly Color TextGray = Color.FromArgb(100, 116, 139);

        // Controls
        private Panel pnlMain = null!;
        private Panel pnlLeft = null!;
        private Panel pnlRight = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private TextBox txtConfirmPassword = null!;
        private TextBox txtFullName = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private Button btnRegister = null!;
        private LinkLabel lnkLogin = null!;
        private Label lblError = null!;

        public frmRegister()
        {
            InitializeComponent();
            SetupForm();
            CreateControls();
        }

        private void SetupForm()
        {
            this.Text = "Đăng ký tài khoản - Quản Lý Nhà Trọ";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            // Main Panel
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // Left Panel - Gradient Background
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,  // 40% of 900px
                BackColor = PrimaryBlue
            };
            pnlLeft.Paint += PnlLeft_Paint;  // Gradient paint

            var lblWelcome = new Label
            {
                Text = "CHÀO MỪNG\nBẠN MỚI",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(340, 150),
                Location = new Point(10, 180),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSubtitle = new Label
            {
                Text = "Tạo tài khoản để bắt đầu sử dụng\nHệ thống quản lý nhà trọ",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 235, 255),
                AutoSize = false,
                Size = new Size(340, 70),
                Location = new Point(10, 340),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlLeft.Controls.AddRange(new Control[] { lblWelcome, lblSubtitle });

            // Right Panel - Register Form
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(40, 30, 40, 30),
                AutoScroll = true
            };

            var lblTitle = new Label
            {
                Text = "Đăng ký",
                Font = new Font("Segoe UI", 30, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(50, 25),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "Điền thông tin để tạo tài khoản mới",
                Font = new Font("Segoe UI", 11.5f),
                ForeColor = TextGray,
                Location = new Point(50, 70),
                AutoSize = true
            };

            int yPos = 110;
            int spacing = 70;

            // Full Name
            var lblFullName = CreateLabel("Họ và tên *", 50, yPos);
            txtFullName = CreateStyledTextBox(50, yPos + 25, false);
            yPos += spacing;

            // Username
            var lblUsername = CreateLabel("Tên đăng nhập *", 50, yPos);
            txtUsername = CreateStyledTextBox(50, yPos + 25, false);
            yPos += spacing;

            // Email
            var lblEmail = CreateLabel("Email *", 50, yPos);
            txtEmail = CreateStyledTextBox(50, yPos + 25, false);
            txtEmail.PlaceholderText = "example@email.com";
            yPos += spacing;

            // Phone
            var lblPhone = CreateLabel("Số điện thoại *", 50, yPos);
            txtPhone = CreateStyledTextBox(50, yPos + 25, false);
            txtPhone.PlaceholderText = "0123456789";
            yPos += spacing;

            // Password
            var lblPassword = CreateLabel("Mật khẩu *", 50, yPos);
            txtPassword = CreateStyledTextBox(50, yPos + 25, true);
            yPos += spacing;

            // Confirm Password
            var lblConfirmPassword = CreateLabel("Xác nhận mật khẩu *", 50, yPos);
            txtConfirmPassword = CreateStyledTextBox(50, yPos + 25, true);
            yPos += spacing;

            // Error label
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(239, 68, 68),
                Location = new Point(50, yPos),
                Size = new Size(360, 35),
                Visible = false
            };
            yPos += 45;

            // Register button
            btnRegister = new Button
            {
                Text = "ĐĂNG KÝ",
                Location = new Point(50, yPos),
                Size = new Size(360, 48),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = SuccessGreen,  // Green color for register
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.MouseEnter += (s, e) => btnRegister.BackColor = HoverGreen;
            btnRegister.MouseLeave += (s, e) => btnRegister.BackColor = SuccessGreen;
            btnRegister.Click += BtnRegister_Click;
            yPos += 65;

            // Login link
            var lblHaveAccount = new Label
            {
                Text = "Đã có tài khoản?",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextGray,
                Location = new Point(130, yPos),
                AutoSize = true
            };

            lnkLogin = new LinkLabel
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(248, yPos),
                AutoSize = true,
                LinkColor = PrimaryBlue,
                ActiveLinkColor = PrimaryBlueDark,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkLogin.Click += LnkLogin_Click;

            pnlRight.Controls.AddRange(new Control[]
            {
                lblTitle, lblDesc, lblFullName, txtFullName,
                lblUsername, txtUsername, lblEmail, txtEmail,
                lblPhone, txtPhone, lblPassword, txtPassword,
                lblConfirmPassword, txtConfirmPassword,
                lblError, btnRegister, lblHaveAccount, lnkLogin
            });

            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlLeft);
            this.Controls.Add(pnlMain);

            // Event handlers
            txtFullName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtUsername.Focus(); };
            txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtEmail.Focus(); };
            txtEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPhone.Focus(); };
            txtPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPassword.Focus(); };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtConfirmPassword.Focus(); };
            txtConfirmPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnRegister_Click(s, e); };

            // Focus
            this.ActiveControl = txtFullName;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private TextBox CreateStyledTextBox(int x, int y, bool isPassword)
        {
            var textBox = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(360, 42),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            if (isPassword)
                textBox.PasswordChar = '●';

            // Add focus events for visual feedback
            textBox.GotFocus += (s, e) =>
            {
                textBox.BackColor = Color.FromArgb(250, 250, 250);
            };
            textBox.LostFocus += (s, e) =>
            {
                textBox.BackColor = Color.White;
            };

            return textBox;
        }

        // Gradient paint for left panel
        private void PnlLeft_Paint(object? sender, PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(
                pnlLeft.ClientRectangle,
                PrimaryBlue,        // #1E88E5
                PrimaryBlueDark,    // #1976D2
                45f))  // 45 degree angle
            {
                e.Graphics.FillRectangle(brush, pnlLeft.ClientRectangle);
            }
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            lblError.Visible = false;
            btnRegister.Enabled = false;
            btnRegister.Text = "Đang xử lý...";

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    ShowError("Vui lòng nhập họ và tên!");
                    txtFullName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    ShowError("Vui lòng nhập tên đăng nhập!");
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    ShowError("Vui lòng nhập email!");
                    txtEmail.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPhone.Text))
                {
                    ShowError("Vui lòng nhập số điện thoại!");
                    txtPhone.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    ShowError("Vui lòng nhập mật khẩu!");
                    txtPassword.Focus();
                    return;
                }

                if (txtPassword.Text != txtConfirmPassword.Text)
                {
                    ShowError("Mật khẩu xác nhận không khớp!");
                    txtConfirmPassword.Focus();
                    return;
                }

                // Call register service
                var (success, message) = await _authService.RegisterAsync(
                    username: txtUsername.Text.Trim(),
                    password: txtPassword.Text,
                    fullName: txtFullName.Text.Trim(),
                    email: txtEmail.Text.Trim(),
                    phone: txtPhone.Text.Trim()
                );

                if (success)
                {
                    UIHelper.ShowSuccess(message);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                btnRegister.Enabled = true;
                btnRegister.Text = "ĐĂNG KÝ";
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        private void LnkLogin_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(900, 650);
            this.Name = "frmRegister";
            this.ResumeLayout(false);
        }
    }
}
