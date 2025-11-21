using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyRoom : UserControl
    {
        private readonly PhongTroRepository _phongRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly TaiSanRepository _taiSanRepo = new();
        private readonly int _tenantUserId;

        private Panel pnlRoomInfo = null!;
        private DataGridView dgvTaiSan = null!;

        public ucMyRoom(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            CreateLayout();
            LoadDataAsync();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;
            this.AutoScroll = true;

            // Header
            var lblTitle = new Label
            {
                Text = "Phòng của tôi",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Room Info Panel
            pnlRoomInfo = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(500, 350),
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };

            var lblRoomTitle = new Label
            {
                Text = "Thông tin phòng",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.Primary,
                Location = new Point(20, 15),
                AutoSize = true
            };
            pnlRoomInfo.Controls.Add(lblRoomTitle);
            this.Controls.Add(pnlRoomInfo);

            // Tài sản Panel
            var lblTaiSan = new Label
            {
                Text = "Tài sản trong phòng",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(540, 60),
                AutoSize = true
            };
            this.Controls.Add(lblTaiSan);

            var pnlTaiSan = new Panel
            {
                Location = new Point(540, 95),
                Size = new Size(500, 315),
                BackColor = ThemeManager.Surface
            };

            dgvTaiSan = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgvTaiSan);
            UIHelper.AddColumn(dgvTaiSan, "TenTaiSan", "Tên tài sản", "TenTaiSan", 180);
            UIHelper.AddColumn(dgvTaiSan, "SoLuong", "Số lượng", "SoLuong", 80);
            UIHelper.AddColumn(dgvTaiSan, "TinhTrang", "Tình trạng", "TinhTrang", 120);
            UIHelper.AddColumn(dgvTaiSan, "GhiChu", "Ghi chú", "GhiChu", 100);
            pnlTaiSan.Controls.Add(dgvTaiSan);
            this.Controls.Add(pnlTaiSan);
        }

        private void AddInfoRow(string label, string value, int y)
        {
            var lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(20, y),
                Size = new Size(150, 25)
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(180, y),
                AutoSize = true
            };

            pnlRoomInfo.Controls.Add(lblLabel);
            pnlRoomInfo.Controls.Add(lblValue);
        }

        private async void LoadDataAsync()
        {
            try
            {
                // Lấy hợp đồng active
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_tenantUserId);
                if (contract == null)
                {
                    AddInfoRow("Trạng thái:", "Bạn chưa có hợp đồng thuê phòng", 60);
                    return;
                }

                // Lấy thông tin phòng
                var room = await _phongRepo.GetByMaPhongAsync(contract.MaPhong);
                if (room == null)
                {
                    AddInfoRow("Trạng thái:", "Không tìm thấy thông tin phòng", 60);
                    return;
                }

                // Hiển thị thông tin
                int y = 55;
                AddInfoRow("Mã phòng:", room.MaPhong, y); y += 35;
                AddInfoRow("Tòa nhà:", room.BuildingName ?? "N/A", y); y += 35;
                AddInfoRow("Loại phòng:", room.TenLoai ?? "N/A", y); y += 35;
                AddInfoRow("Tầng:", room.Tang.ToString(), y); y += 35;
                AddInfoRow("Diện tích:", $"{room.DienTich} m²", y); y += 35;
                AddInfoRow("Giá thuê:", $"{room.GiaThue:N0} VNĐ/tháng", y); y += 35;
                AddInfoRow("Số người tối đa:", room.SoNguoiToiDa.ToString(), y); y += 35;
                AddInfoRow("Trạng thái:", room.TrangThai, y);

                // Lấy tài sản trong phòng
                var taiSans = await _taiSanRepo.GetByPhongIdAsync(room.PhongId);
                dgvTaiSan.DataSource = taiSans.ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyRoom";
            this.Size = new Size(1100, 700);
        }
    }
}
