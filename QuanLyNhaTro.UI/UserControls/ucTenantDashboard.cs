using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucTenantDashboard : UserControl
    {
        private readonly HoaDonRepository _hoaDonRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly BaoTriRepository _baoTriRepo = new();
        private readonly int _tenantUserId;

        private Panel pnlStats = null!;
        private DataGridView dgvHoaDon = null!, dgvTicket = null!;

        public ucTenantDashboard(int tenantUserId)
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
                Text = "Dashboard - Khách thuê",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Stats Panel
            pnlStats = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(1050, 100),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlStats);

            // Hóa đơn gần nhất
            var lblHoaDon = new Label
            {
                Text = "Hóa đơn gần nhất",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(20, 175),
                AutoSize = true
            };
            this.Controls.Add(lblHoaDon);

            var pnlHoaDon = new Panel
            {
                Location = new Point(20, 205),
                Size = new Size(500, 250),
                BackColor = ThemeManager.Surface
            };
            dgvHoaDon = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgvHoaDon);
            UIHelper.AddColumn(dgvHoaDon, "MaHoaDon", "Mã HĐ", "MaHoaDon", 80);
            UIHelper.AddColumn(dgvHoaDon, "ThangNam", "Tháng", "ThangNam", 80);
            UIHelper.AddColumn(dgvHoaDon, "TongCong", "Tổng tiền", "TongCong", 100);
            UIHelper.AddColumn(dgvHoaDon, "TrangThai", "Trạng thái", "TrangThai", 100);
            UIHelper.AddColumn(dgvHoaDon, "NgayHetHan", "Hạn TT", "NgayHetHan", 90);
            pnlHoaDon.Controls.Add(dgvHoaDon);
            this.Controls.Add(pnlHoaDon);

            // Ticket bảo trì gần nhất
            var lblTicket = new Label
            {
                Text = "Yêu cầu bảo trì gần nhất",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(540, 175),
                AutoSize = true
            };
            this.Controls.Add(lblTicket);

            var pnlTicket = new Panel
            {
                Location = new Point(540, 205),
                Size = new Size(500, 250),
                BackColor = ThemeManager.Surface
            };
            dgvTicket = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgvTicket);
            UIHelper.AddColumn(dgvTicket, "TicketId", "Mã", "TicketId", 60);
            UIHelper.AddColumn(dgvTicket, "TieuDe", "Tiêu đề", "TieuDe", 150);
            UIHelper.AddColumn(dgvTicket, "TrangThai", "Trạng thái", "TrangThai", 100);
            UIHelper.AddColumn(dgvTicket, "NgayTao", "Ngày gửi", "NgayTao", 90);
            pnlTicket.Controls.Add(dgvTicket);
            this.Controls.Add(pnlTicket);
        }

        private Panel CreateStatCard(string title, string value, Color color, int x)
        {
            var card = new Panel
            {
                Location = new Point(x, 0),
                Size = new Size(195, 90),
                BackColor = ThemeManager.Surface
            };

            var indicator = new Panel { Size = new Size(5, 90), BackColor = color, Dock = DockStyle.Left };
            card.Controls.Add(indicator);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(15, 12),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(15, 40),
                AutoSize = true
            };
            card.Controls.Add(lblValue);

            return card;
        }

        private async void LoadDataAsync()
        {
            try
            {
                // Lấy thông tin hợp đồng active của tenant
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_tenantUserId);

                decimal tienPhong = 0;
                int ngayConLai = 0;
                decimal congNo = 0;
                int hoaDonChuaTT = 0;
                int ticketChoXuLy = 0;

                if (contract != null)
                {
                    tienPhong = contract.GiaThue;
                    ngayConLai = (contract.NgayKetThuc - DateTime.Now).Days;
                    if (ngayConLai < 0) ngayConLai = 0;

                    // Lấy hóa đơn của tenant
                    var hoaDons = await _hoaDonRepo.GetByContractAsync(contract.MaHopDong);
                    var hoaDonList = hoaDons.ToList();
                    hoaDonChuaTT = hoaDonList.Count(h => h.TrangThai != "DaThanhToan");
                    congNo = hoaDonList.Sum(h => h.ConNo);

                    dgvHoaDon.DataSource = hoaDonList.Take(5).ToList();

                    // Lấy ticket của tenant
                    var tickets = await _baoTriRepo.GetByTenantAsync(contract.KhachId);
                    var ticketList = tickets.ToList();
                    ticketChoXuLy = ticketList.Count(t => t.TrangThai == "Mới" || t.TrangThai == "Đang xử lý");
                    dgvTicket.DataSource = ticketList.Take(5).ToList();
                }

                // Tạo stat cards
                pnlStats.Controls.Clear();
                pnlStats.Controls.Add(CreateStatCard("Tiền phòng/tháng", $"{tienPhong:N0}đ", Color.FromArgb(59, 130, 246), 0));
                pnlStats.Controls.Add(CreateStatCard("HĐ chưa thanh toán", hoaDonChuaTT.ToString(), Color.FromArgb(245, 158, 11), 210));
                pnlStats.Controls.Add(CreateStatCard("Ngày còn lại HĐ", ngayConLai.ToString(), Color.FromArgb(16, 185, 129), 420));
                pnlStats.Controls.Add(CreateStatCard("Công nợ hiện tại", $"{congNo:N0}đ", Color.FromArgb(239, 68, 68), 630));
                pnlStats.Controls.Add(CreateStatCard("Ticket chờ xử lý", ticketChoXuLy.ToString(), Color.FromArgb(168, 85, 247), 840));

                // Format columns
                foreach (DataGridViewColumn col in dgvHoaDon.Columns)
                {
                    if (col.Name == "TongCong") col.DefaultCellStyle.Format = "N0";
                    if (col.Name == "ThangNam" || col.Name == "NgayHetHan") col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
                foreach (DataGridViewColumn col in dgvTicket.Columns)
                {
                    if (col.Name == "NgayTao") col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucTenantDashboard";
            this.Size = new Size(1100, 700);
        }
    }
}
