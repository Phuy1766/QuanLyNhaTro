using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.UI.Helpers;
using System.Drawing.Drawing2D;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class frmLogin : Form
    {
        private readonly AuthService _authService = new();

        // Colors - Material Design Blue
        private readonly Color PrimaryBlue = Color.FromArgb(30, 136, 229);      // #1E88E5
        private readonly Color PrimaryBlueDark = Color.FromArgb(25, 118, 210);  // #1976D2
        private readonly Color HoverBlue = Color.FromArgb(21, 101, 192);        // #1565C0
        private readonly Color BorderColor = Color.FromArgb(208, 215, 226);     // #D0D7E2
        private readonly Color TextDark = Color.FromArgb(30, 41, 59);
        private readonly Color TextGray = Color.FromArgb(100, 116, 139);

        // Controls
        private Panel pnlMain = null!;
        private Panel pnlLeft = null!;
        private Panel pnlRight = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private CheckBox chkRemember = null!;
        private Label lblError = null!;
        private LinkLabel lnkRegister = null!;

        public frmLogin()
        {
            InitializeComponent();
            SetupForm();
            CreateControls();
        }

        private void SetupForm()
        {
            this.Text = "Đăng nhập - Quản Lý Nhà Trọ";
            this.Size = new Size(900, 550);
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
                Text = "QUẢN LÝ NHÀ TRỌ",
                Font = new Font("Segoe UI", 38, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(340, 120),
                Location = new Point(10, 160),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSubtitle = new Label
            {
                Text = "Hệ thống quản lý nhà trọ chuyên nghiệp\nPhiên bản 1.0 – 2025",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 235, 255),
                AutoSize = false,
                Size = new Size(340, 70),
                Location = new Point(10, 290),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlLeft.Controls.AddRange(new Control[] { lblWelcome, lblSubtitle });

            // Right Panel - Login Form
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(60, 50, 60, 50)
            };

            var lblTitle = new Label
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(70, 70),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "Vui lòng đăng nhập để tiếp tục",
                Font = new Font("Segoe UI", 11.5f),
                ForeColor = TextGray,
                Location = new Point(70, 125),
                AutoSize = true
            };

            // Username
            var lblUsername = new Label
            {
                Text = "Tên đăng nhập",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(70, 180),
                AutoSize = true
            };

            txtUsername = new TextBox
            {
                Location = new Point(70, 205),
                Size = new Size(340, 42),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            // Apply custom border color
            ApplyTextBoxBorder(txtUsername);

            // Password
            var lblPassword = new Label
            {
                Text = "Mật khẩu",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(70, 265),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(70, 290),
                Size = new Size(340, 42),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                BackColor = Color.White
            };
            ApplyTextBoxBorder(txtPassword);

            // Remember checkbox
            chkRemember = new CheckBox
            {
                Text = "Ghi nhớ đăng nhập",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(70, 345),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            // Error label
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(239, 68, 68),
                Location = new Point(70, 375),
                Size = new Size(340, 25),
                Visible = false
            };

            // Login button
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Location = new Point(70, 410),
                Size = new Size(340, 48),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = PrimaryBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = HoverBlue;
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = PrimaryBlue;
            btnLogin.Click += BtnLogin_Click;

            // Register link
            var lblNoAccount = new Label
            {
                Text = "Chưa có tài khoản?",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(150, 480),
                AutoSize = true
            };

            lnkRegister = new LinkLabel
            {
                Text = "Đăng ký ngay",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(280, 480),
                AutoSize = true,
                LinkColor = PrimaryBlue,
                ActiveLinkColor = HoverBlue,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkRegister.Click += LnkRegister_Click;

            pnlRight.Controls.AddRange(new Control[]
            {
                lblTitle, lblDesc, lblUsername, txtUsername,
                lblPassword, txtPassword, chkRemember, lblError, btnLogin, lblNoAccount, lnkRegister
            });

            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlLeft);
            this.Controls.Add(pnlMain);

            // Event handlers
            txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPassword.Focus(); };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };

            // Focus
            this.ActiveControl = txtUsername;
        }

        private void LnkRegister_Click(object? sender, EventArgs e)
        {
            var registerForm = new frmRegister();
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                UIHelper.ShowSuccess("Đăng ký thành công! Vui lòng đăng nhập.", "Thành công");
            }
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

        // Apply custom border styling to textboxes
        private void ApplyTextBoxBorder(TextBox textBox)
        {
            // Add focus events for border color change
            textBox.GotFocus += (s, e) =>
            {
                textBox.BackColor = Color.FromArgb(250, 250, 250);
            };
            textBox.LostFocus += (s, e) =>
            {
                textBox.BackColor = Color.White;
            };
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Visible = false;
            btnLogin.Enabled = false;
            btnLogin.Text = "Đang đăng nhập...";

            try
            {
                var (success, message) = await _authService.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text);

                if (success)
                {
                    this.Hide();
                    var mainForm = new frmMain();
                    mainForm.FormClosed += (s, args) => this.Close();
                    mainForm.Show();
                }
                else
                {
                    lblError.Text = message;
                    lblError.Visible = true;
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "ĐĂNG NHẬP";
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(900, 550);
            this.Name = "frmLogin";
            this.ResumeLayout(false);
        }
    }
}
