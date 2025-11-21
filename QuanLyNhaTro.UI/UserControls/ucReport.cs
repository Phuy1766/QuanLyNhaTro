using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucReport : UserControl
    {
        private readonly DashboardRepository _dashboardRepo = new();
        private readonly HoaDonRepository _hoaDonRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly PhongTroRepository _phongRepo = new();

        private ComboBox cboReportType = null!;
        private DataGridView dgv = null!;
        private Panel pnlSummary = null!;

        public ucReport()
        {
            InitializeComponent();
            CreateLayout();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ThemeManager.Surface, Padding = new Padding(15, 10, 15, 10) };

            var lblType = new Label { Text = "Loại báo cáo:", Location = new Point(15, 18), AutoSize = true };
            cboReportType = new ComboBox { Location = new Point(100, 14), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboReportType.Items.AddRange(new object[]
            {
                "Phòng trống",
                "Hóa đơn chưa thanh toán",
                "Hóa đơn quá hạn",
                "Hợp đồng sắp hết hạn",
                "Doanh thu theo tháng",
                "Thống kê theo tòa nhà"
            });
            cboReportType.SelectedIndex = 0;
            cboReportType.SelectedIndexChanged += (s, e) => LoadReport();

            var btnExport = new Button { Text = "📥 Xuất Excel", Size = new Size(100, 35), Location = new Point(370, 12), BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnExport.Click += BtnExport_Click;

            pnlToolbar.Controls.AddRange(new Control[] { lblType, cboReportType, btnExport });

            // Summary panel
            pnlSummary = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = ThemeManager.Surface, Padding = new Padding(15), Visible = false };
            this.Controls.Add(pnlSummary);

            // Grid
            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface, Padding = new Padding(15) };
            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgv);
            pnlGrid.Controls.Add(dgv);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(pnlToolbar);

            LoadReport();
        }

        private async void LoadReport()
        {
            dgv.Columns.Clear();
            pnlSummary.Visible = false;

            switch (cboReportType.SelectedIndex)
            {
                case 0: // Phòng trống
                    UIHelper.AddColumn(dgv, "MaPhong", "Mã phòng", "MaPhong", 100);
                    UIHelper.AddColumn(dgv, "BuildingName", "Tòa nhà", "BuildingName", 150);
                    UIHelper.AddColumn(dgv, "TenLoai", "Loại phòng", "TenLoai", 120);
                    UIHelper.AddColumn(dgv, "Tang", "Tầng", "Tang", 60);
                    UIHelper.AddColumn(dgv, "DienTich", "Diện tích", "DienTich", 80);
                    UIHelper.AddColumn(dgv, "GiaThue", "Giá thuê", "GiaThue", 120);
                    var phongTrong = await _phongRepo.GetAvailableRoomsAsync();
                    dgv.DataSource = phongTrong.ToList();
                    break;

                case 1: // Hóa đơn chưa thanh toán
                case 2: // Hóa đơn quá hạn
                    UIHelper.AddColumn(dgv, "MaHoaDon", "Mã HĐ", "MaHoaDon", 100);
                    UIHelper.AddColumn(dgv, "MaPhong", "Phòng", "MaPhong", 80);
                    UIHelper.AddColumn(dgv, "TenKhachThue", "Khách thuê", "TenKhachThue", 150);
                    UIHelper.AddColumn(dgv, "ThangNam", "Tháng", "ThangNam", 80);
                    UIHelper.AddColumn(dgv, "TongCong", "Tổng cộng", "TongCong", 100);
                    UIHelper.AddColumn(dgv, "ConNo", "Còn nợ", "ConNo", 100);
                    UIHelper.AddColumn(dgv, "NgayHetHan", "Hạn TT", "NgayHetHan", 90);

                    var hoaDons = cboReportType.SelectedIndex == 1
                        ? await _hoaDonRepo.GetUnpaidAsync()
                        : await _hoaDonRepo.GetOverdueAsync();
                    dgv.DataSource = hoaDons.ToList();

                    // Summary
                    pnlSummary.Visible = true;
                    pnlSummary.Controls.Clear();
                    var totalDebt = hoaDons.Sum(h => h.ConNo);
                    pnlSummary.Controls.Add(new Label
                    {
                        Text = $"Tổng số: {hoaDons.Count()} hóa đơn | Tổng công nợ: {totalDebt:N0} VNĐ",
                        Font = new Font("Segoe UI", 12, FontStyle.Bold),
                        ForeColor = Color.FromArgb(239, 68, 68),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    });
                    break;

                case 3: // Hợp đồng sắp hết hạn
                    UIHelper.AddColumn(dgv, "MaHopDong", "Mã HĐ", "MaHopDong", 100);
                    UIHelper.AddColumn(dgv, "MaPhong", "Phòng", "MaPhong", 80);
                    UIHelper.AddColumn(dgv, "TenKhachThue", "Khách thuê", "TenKhachThue", 150);
                    UIHelper.AddColumn(dgv, "Phone", "SĐT", "Phone", 100);
                    UIHelper.AddColumn(dgv, "NgayKetThuc", "Ngày hết hạn", "NgayKetThuc", 100);
                    UIHelper.AddColumn(dgv, "SoNgayConLai", "Còn lại (ngày)", "SoNgayConLai", 100);
                    var expiring = await _hopDongRepo.GetExpiringSoonAsync(30);
                    dgv.DataSource = expiring.ToList();
                    break;

                case 4: // Doanh thu theo tháng
                    UIHelper.AddColumn(dgv, "Thang", "Tháng", "Thang", 150);
                    UIHelper.AddColumn(dgv, "DoanhThu", "Doanh thu", "DoanhThu", 200);
                    var revenue = await _dashboardRepo.GetRevenueByYearAsync(DateTime.Now.Year);
                    dgv.DataSource = revenue.ToList();
                    break;

                case 5: // Thống kê theo tòa nhà
                    UIHelper.AddColumn(dgv, "BuildingName", "Tòa nhà", "BuildingName", 200);
                    UIHelper.AddColumn(dgv, "TongPhong", "Tổng phòng", "TongPhong", 100);
                    UIHelper.AddColumn(dgv, "PhongTrong", "Phòng trống", "PhongTrong", 100);
                    UIHelper.AddColumn(dgv, "PhongDangThue", "Đang thuê", "PhongDangThue", 100);
                    var buildingStats = await _dashboardRepo.GetRoomStatsByBuildingAsync();
                    dgv.DataSource = buildingStats.ToList();
                    break;
            }

            // Format columns
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name == "GiaThue" || col.Name == "TongCong" || col.Name == "ConNo" || col.Name == "DoanhThu")
                    col.DefaultCellStyle.Format = "N0";
                if (col.Name == "ThangNam" || col.Name == "NgayKetThuc" || col.Name == "NgayHetHan")
                    col.DefaultCellStyle.Format = "dd/MM/yyyy";
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            UIHelper.ShowWarning("Tính năng xuất Excel cần thêm thư viện EPPlus.\nVui lòng tham khảo code mẫu trong dự án.");
        }

        private void InitializeComponent() { this.Name = "ucReport"; this.Size = new Size(1100, 700); }
    }
}
