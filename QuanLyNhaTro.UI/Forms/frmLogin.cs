using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.UI.Helpers;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class frmLogin : Form
    {
        private readonly AuthService _authService = new();

        // Controls
        private Panel pnlMain = null!;
        private Panel pnlLeft = null!;
        private Panel pnlRight = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private CheckBox chkRemember = null!;
        private Label lblError = null!;

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

            // Left Panel - Decorative
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 400,
                BackColor = Color.FromArgb(59, 130, 246)
            };

            var lblWelcome = new Label
            {
                Text = "QUẢN LÝ\nNHÀ TRỌ",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(360, 150),
                Location = new Point(20, 150),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSubtitle = new Label
            {
                Text = "Hệ thống quản lý nhà trọ chuyên nghiệp\nPhiên bản 1.0 - 2025",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(200, 220, 255),
                AutoSize = false,
                Size = new Size(360, 60),
                Location = new Point(20, 310),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlLeft.Controls.AddRange(new Control[] { lblWelcome, lblSubtitle });

            // Right Panel - Login Form
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(50)
            };

            var lblTitle = new Label
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(80, 80),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "Vui lòng đăng nhập để tiếp tục",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(80, 130),
                AutoSize = true
            };

            // Username
            var lblUsername = new Label
            {
                Text = "Tên đăng nhập",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(80, 190),
                AutoSize = true
            };

            txtUsername = new TextBox
            {
                Location = new Point(80, 215),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Password
            var lblPassword = new Label
            {
                Text = "Mật khẩu",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(80, 270),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(80, 295),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●'
            };

            // Remember checkbox
            chkRemember = new CheckBox
            {
                Text = "Ghi nhớ đăng nhập",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(80, 345),
                AutoSize = true
            };

            // Error label
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(239, 68, 68),
                Location = new Point(80, 375),
                Size = new Size(300, 25),
                Visible = false
            };

            // Login button
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Location = new Point(80, 410),
                Size = new Size(300, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Default account hint
            var lblHint = new Label
            {
                Text = "Tài khoản mặc định: admin / 123456",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(80, 470),
                AutoSize = true
            };

            pnlRight.Controls.AddRange(new Control[]
            {
                lblTitle, lblDesc, lblUsername, txtUsername,
                lblPassword, txtPassword, chkRemember, lblError, btnLogin, lblHint
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
