using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;
using System.Text.RegularExpressions;

namespace QuanLyNhaTro.UI.UserControls
{
    /// <summary>
    /// Trang Thông tin cá nhân (Tenant)
    /// </summary>
    public partial class ucMyProfile : UserControl
    {
        private readonly KhachThueRepository _khachRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _userId;

        // Controls
        private TextBox txtHoTen = null!;
        private TextBox txtSDT = null!;
        private TextBox txtEmail = null!;
        private TextBox txtCCCD = null!;
        private DateTimePicker dtpNgaySinh = null!;
        private TextBox txtDiaChi = null!;
        private TextBox txtNgheNghiep = null!;
        private Button btnSave = null!;

        // Info labels
        private Label lblMaKhach = null!;
        private Label lblPhongThue = null!;
        private Label lblNgayVao = null!;
        private Label lblNgayHetHan = null!;

        public ucMyProfile(int userId)
        {
            _userId = userId;
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;
            this.Padding = new Padding(0);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            // ===== AVATAR & BASIC INFO CARD =====
            var pnlTop = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(900, 160),
                BackColor = ThemeManager.Surface
            };
            UIHelper.RoundControl(pnlTop, 16);

            // Avatar
            var pnlAvatar = new Panel
            {
                Location = new Point(30, 30),
                Size = new Size(100, 100),
                BackColor = ThemeManager.Primary
            };
            UIHelper.RoundControl(pnlAvatar, 50);

            var lblAvatarInitial = new Label
            {
                Text = AuthService.CurrentUser?.FullName?.Substring(0, 1).ToUpper() ?? "T",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            pnlAvatar.Controls.Add(lblAvatarInitial);
            pnlTop.Controls.Add(pnlAvatar);

            // Basic info
            var lblName = new Label
            {
                Text = AuthService.CurrentUser?.FullName ?? "Khách thuê",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(150, 35),
                AutoSize = true
            };

            var lblRole = new Label
            {
                Text = "🏠 Khách thuê nhà trọ",
                Font = new Font("Segoe UI", 11),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(150, 70),
                AutoSize = true
            };

            lblMaKhach = new Label
            {
                Text = "Mã: ...",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextMuted,
                Location = new Point(150, 95),
                AutoSize = true
            };

            pnlTop.Controls.AddRange(new Control[] { lblName, lblRole, lblMaKhach });

            // Contract Info
            var pnlContractInfo = new Panel
            {
                Location = new Point(500, 25),
                Size = new Size(380, 110),
                BackColor = Color.FromArgb(30, ThemeManager.Primary)
            };
            UIHelper.RoundControl(pnlContractInfo, 12);

            lblPhongThue = new Label
            {
                Text = "Phòng: Chưa thuê",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(15, 15),
                AutoSize = true
            };

            lblNgayVao = new Label
            {
                Text = "Ngày vào: --/--/----",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(15, 45),
                AutoSize = true
            };

            lblNgayHetHan = new Label
            {
                Text = "Hết hạn: --/--/----",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(15, 75),
                AutoSize = true
            };

            pnlContractInfo.Controls.AddRange(new Control[] { lblPhongThue, lblNgayVao, lblNgayHetHan });
            pnlTop.Controls.Add(pnlContractInfo);

            // ===== FORM EDIT CARD =====
            var pnlForm = new Panel
            {
                Location = new Point(0, 175),
                Size = new Size(900, 420),
                BackColor = ThemeManager.Surface,
                Padding = new Padding(30)
            };
            UIHelper.RoundControl(pnlForm, 16);

            var lblFormTitle = new Label
            {
                Text = "✏️ Thông tin cá nhân",
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };
            pnlForm.Controls.Add(lblFormTitle);

            int y = 45;
            int col1X = 0, col2X = 430;
            int lblW = 100, txtW = 280;

            // Row 1: Họ tên, SĐT
            AddFormField(pnlForm, "Họ tên:", col1X, y, lblW, out _, out txtHoTen, txtW);
            AddFormField(pnlForm, "Số điện thoại:", col2X, y, lblW, out _, out txtSDT, txtW);
            y += 50;

            // Row 2: Email, CCCD
            AddFormField(pnlForm, "Email:", col1X, y, lblW, out _, out txtEmail, txtW);
            AddFormField(pnlForm, "CCCD/CMND:", col2X, y, lblW, out _, out txtCCCD, txtW);
            y += 50;

            // Row 3: Ngày sinh, Nghề nghiệp
            var lblNgaySinh = new Label
            {
                Text = "Ngày sinh:",
                Location = new Point(col1X, y + 3),
                Width = lblW,
                ForeColor = ThemeManager.TextPrimary
            };
            dtpNgaySinh = new DateTimePicker
            {
                Location = new Point(col1X + lblW + 10, y),
                Size = new Size(txtW, 30),
                Format = DateTimePickerFormat.Short
            };
            pnlForm.Controls.AddRange(new Control[] { lblNgaySinh, dtpNgaySinh });

            AddFormField(pnlForm, "Nghề nghiệp:", col2X, y, lblW, out _, out txtNgheNghiep, txtW);
            y += 50;

            // Row 4: Địa chỉ
            var lblDiaChi = new Label
            {
                Text = "Địa chỉ:",
                Location = new Point(col1X, y + 3),
                Width = lblW,
                ForeColor = ThemeManager.TextPrimary
            };
            txtDiaChi = new TextBox
            {
                Location = new Point(col1X + lblW + 10, y),
                Size = new Size(720, 60),
                Multiline = true
            };
            pnlForm.Controls.AddRange(new Control[] { lblDiaChi, txtDiaChi });
            y += 80;

            // Save button
            btnSave = new Button
            {
                Text = "💾 Lưu thay đổi",
                Location = new Point(col1X + lblW + 10, y),
                Size = new Size(150, 45),
                BackColor = ThemeManager.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnSave, 10);
            btnSave.Click += BtnSave_Click;
            pnlForm.Controls.Add(btnSave);

            scrollPanel.Controls.Add(pnlForm);
            scrollPanel.Controls.Add(pnlTop);

            this.Controls.Add(scrollPanel);
        }

        private void AddFormField(Panel parent, string label, int x, int y, int lblWidth, out Label lbl, out TextBox txt, int txtWidth)
        {
            lbl = new Label
            {
                Text = label,
                Location = new Point(x, y + 3),
                Width = lblWidth,
                ForeColor = ThemeManager.TextPrimary
            };
            txt = new TextBox
            {
                Location = new Point(x + lblWidth + 10, y),
                Size = new Size(txtWidth, 30)
            };
            parent.Controls.AddRange(new Control[] { lbl, txt });
        }

        private async void LoadData()
        {
            try
            {
                // Load khách thuê theo UserId
                var khach = await _khachRepo.GetByUserIdAsync(_userId);
                if (khach != null)
                {
                    lblMaKhach.Text = $"Mã: {khach.MaKhach}";
                    txtHoTen.Text = khach.HoTen;
                    txtSDT.Text = khach.Phone;
                    txtEmail.Text = khach.Email;
                    txtCCCD.Text = khach.CCCD;
                    if (khach.NgaySinh.HasValue)
                        dtpNgaySinh.Value = khach.NgaySinh.Value;
                    txtDiaChi.Text = khach.DiaChi;
                    txtNgheNghiep.Text = khach.NgheNghiep;
                }

                // Load hợp đồng hiện tại
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_userId);
                if (contract != null)
                {
                    lblPhongThue.Text = $"🚪 Phòng: {contract.MaPhong}";
                    lblNgayVao.Text = $"📅 Ngày vào: {contract.NgayBatDau:dd/MM/yyyy}";
                    lblNgayHetHan.Text = $"⏰ Hết hạn: {contract.NgayKetThuc:dd/MM/yyyy}";

                    var daysLeft = (contract.NgayKetThuc - DateTime.Today).Days;
                    if (daysLeft < 30)
                    {
                        lblNgayHetHan.ForeColor = ThemeManager.Warning;
                        lblNgayHetHan.Text += $" ({daysLeft} ngày)";
                    }
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải thông tin: {ex.Message}");
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                UIHelper.ShowWarning("Vui lòng nhập họ tên!");
                txtHoTen.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtSDT.Text) && !Regex.IsMatch(txtSDT.Text, @"^0\d{9,10}$"))
            {
                UIHelper.ShowWarning("Số điện thoại không hợp lệ!");
                txtSDT.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !Regex.IsMatch(txtEmail.Text, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                UIHelper.ShowWarning("Email không hợp lệ!");
                txtEmail.Focus();
                return;
            }

            try
            {
                var khach = await _khachRepo.GetByUserIdAsync(_userId);
                if (khach == null)
                {
                    UIHelper.ShowError("Không tìm thấy thông tin khách thuê!");
                    return;
                }

                khach.HoTen = txtHoTen.Text.Trim();
                khach.Phone = txtSDT.Text.Trim();
                khach.Email = txtEmail.Text.Trim();
                khach.CCCD = txtCCCD.Text.Trim();
                khach.NgaySinh = dtpNgaySinh.Value;
                khach.DiaChi = txtDiaChi.Text.Trim();
                khach.NgheNghiep = txtNgheNghiep.Text.Trim();

                await _khachRepo.UpdateAsync(khach);
                UIHelper.ShowSuccess("Cập nhật thông tin thành công!");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi cập nhật: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucMyProfile";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }
}
