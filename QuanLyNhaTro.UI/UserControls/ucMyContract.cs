using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyContract : UserControl
    {
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _tenantUserId;

        private Panel pnlContract = null!;
        private DataGridView dgvHistory = null!;

        public ucMyContract(int tenantUserId)
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
                Text = "Hợp đồng của tôi",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Contract Info Panel
            pnlContract = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(500, 380),
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };

            var lblContractTitle = new Label
            {
                Text = "Thông tin hợp đồng hiện tại",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.Primary,
                Location = new Point(20, 15),
                AutoSize = true
            };
            pnlContract.Controls.Add(lblContractTitle);
            this.Controls.Add(pnlContract);

            // Contract History Panel
            var lblHistory = new Label
            {
                Text = "Lịch sử hợp đồng",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(540, 60),
                AutoSize = true
            };
            this.Controls.Add(lblHistory);

            var pnlHistory = new Panel
            {
                Location = new Point(540, 95),
                Size = new Size(500, 345),
                BackColor = ThemeManager.Surface
            };

            dgvHistory = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgvHistory);
            UIHelper.AddColumn(dgvHistory, "MaHopDong", "Mã HĐ", "MaHopDong", 80);
            UIHelper.AddColumn(dgvHistory, "NgayBatDau", "Ngày BĐ", "NgayBatDau", 90);
            UIHelper.AddColumn(dgvHistory, "NgayKetThuc", "Ngày KT", "NgayKetThuc", 90);
            UIHelper.AddColumn(dgvHistory, "GiaThue", "Giá thuê", "GiaThue", 100);
            UIHelper.AddColumn(dgvHistory, "TrangThai", "Trạng thái", "TrangThai", 90);
            pnlHistory.Controls.Add(dgvHistory);
            this.Controls.Add(pnlHistory);
        }

        private void AddInfoRow(string label, string value, int y, Color? valueColor = null)
        {
            var lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(20, y),
                Size = new Size(160, 25)
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = valueColor ?? ThemeManager.TextPrimary,
                Location = new Point(190, y),
                AutoSize = true
            };

            pnlContract.Controls.Add(lblLabel);
            pnlContract.Controls.Add(lblValue);
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

                // Hiển thị thông tin hợp đồng
                int y = 55;
                AddInfoRow("Mã hợp đồng:", $"#{contract.MaHopDong}", y); y += 35;
                AddInfoRow("Phòng:", contract.MaPhong, y); y += 35;
                AddInfoRow("Tiền cọc:", $"{contract.TienCoc:N0} VNĐ", y); y += 35;
                AddInfoRow("Giá thuê:", $"{contract.GiaThue:N0} VNĐ/tháng", y); y += 35;
                AddInfoRow("Ngày bắt đầu:", contract.NgayBatDau.ToString("dd/MM/yyyy"), y); y += 35;
                AddInfoRow("Ngày kết thúc:", contract.NgayKetThuc.ToString("dd/MM/yyyy"), y); y += 35;

                // Tính số ngày còn lại
                var daysRemaining = (contract.NgayKetThuc - DateTime.Now).Days;
                Color statusColor;
                string statusText;

                if (contract.TrangThai != "Active")
                {
                    statusText = "Đã kết thúc";
                    statusColor = Color.Gray;
                }
                else if (daysRemaining < 0)
                {
                    statusText = "Đã hết hạn";
                    statusColor = Color.FromArgb(239, 68, 68);
                }
                else if (daysRemaining <= 30)
                {
                    statusText = $"Sắp hết hạn ({daysRemaining} ngày)";
                    statusColor = Color.FromArgb(245, 158, 11);
                }
                else
                {
                    statusText = $"Còn hiệu lực ({daysRemaining} ngày)";
                    statusColor = Color.FromArgb(16, 185, 129);
                }

                AddInfoRow("Trạng thái:", statusText, y, statusColor); y += 35;
                AddInfoRow("Chu kỳ thanh toán:", $"{contract.ChuKyThanhToan} tháng", y);

                // Lấy lịch sử hợp đồng
                var allContracts = await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId);
                dgvHistory.DataSource = allContracts.ToList();

                // Format columns
                foreach (DataGridViewColumn col in dgvHistory.Columns)
                {
                    if (col.Name == "GiaThue") col.DefaultCellStyle.Format = "N0";
                    if (col.Name == "NgayBatDau" || col.Name == "NgayKetThuc")
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyContract";
            this.Size = new Size(1100, 700);
        }
    }
}
