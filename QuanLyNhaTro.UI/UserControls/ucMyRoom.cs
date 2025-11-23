using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System.Drawing;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyRoom : UserControl
    {
        private readonly PhongTroRepository _phongRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly TaiSanRepository _taiSanRepo = new();
        private readonly int _tenantUserId;

        private ModernDataGrid dgvRooms = null!;
        private Label lblSummary = null!;
        private Label lblEmptyMessage = null!;
        private Panel pnlMainCard = null!;

        public ucMyRoom(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            BuildModernUI();
            LoadDataAsync();
        }

        private void BuildModernUI()
        {
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.Padding = new Padding(20);

            // Card chính
            pnlMainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30),
                BorderStyle = BorderStyle.None
            };
            UIHelper.ApplyCardShadow(pnlMainCard);

            // Header
            var pnlHeader = new Panel { Height = 80, Dock = DockStyle.Top };
            var lblIcon = new Label
            {
                Text = "🏠",
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(0, 15),
                AutoSize = true
            };
            var lblTitle = new Label
            {
                Text = "Phòng của tôi",
                Font = new Font("Segoe UI Semibold", 24F),
                ForeColor = Color.FromArgb(52, 58, 64),
                Location = new Point(80, 22),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange(new Control[] { lblIcon, lblTitle });

            // Summary
            var pnlSummary = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(0, 122, 255)
            };
            lblSummary = new Label
            {
                Text = "Đang tải dữ liệu...",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(30, 0, 0, 0)
            };
            pnlSummary.Controls.Add(lblSummary);

            // DataGrid
            dgvRooms = new ModernDataGrid
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 56,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(52, 58, 64),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 11F),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(16, 12, 16, 12),
                    Font = new Font("Segoe UI", 11F),
                    SelectionBackColor = Color.FromArgb(0, 122, 255),
                    SelectionForeColor = Color.White
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(248, 249, 250)
                }
            };

            // Hover row
            dgvRooms.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvRooms.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
            };
            dgvRooms.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvRooms.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 249, 250);
            };

            // Cột
            UIHelper.AddColumn(dgvRooms, "MaPhong", "Mã phòng", "MaPhong", 110);
            UIHelper.AddColumn(dgvRooms, "BuildingName", "Tòa nhà", "BuildingName", 150);
            UIHelper.AddColumn(dgvRooms, "NgayBatDau", "Từ ngày", "NgayBatDau", 120);
            UIHelper.AddColumn(dgvRooms, "NgayKetThuc", "Đến ngày", "NgayKetThuc", 120);
            UIHelper.AddColumn(dgvRooms, "GiaThue", "Giá thuê", "GiaThue", 130);
            UIHelper.AddColumn(dgvRooms, "TrangThaiHopDong", "Trạng thái", "TrangThaiHopDong", 150);

            var btnDetail = new DataGridViewButtonColumn
            {
                Name = "btnDetail",
                HeaderText = "Chi tiết",
                Text = "Xem chi tiết",
                UseColumnTextForButtonValue = true,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(0, 122, 255),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                }
            };
            dgvRooms.Columns.Add(btnDetail);

            dgvRooms.CellClick += DgvRooms_CellClick;
            dgvRooms.CellFormatting += DgvRooms_CellFormatting;

            // Empty message
            lblEmptyMessage = new Label
            {
                Font = new Font("Segoe UI", 16F),
                ForeColor = Color.FromArgb(149, 165, 166),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };

            // Layout
            pnlMainCard.Controls.AddRange(new Control[] { dgvRooms, lblEmptyMessage, pnlSummary, pnlHeader });
            this.Controls.Add(pnlMainCard);
        }

        private async void LoadDataAsync()
        {
            try
            {
                var contracts = (await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId))
                    .Where(c => c.TrangThai == "Active")
                    .OrderBy(c => c.MaPhong)
                    .ToList();

                if (contracts.Count == 0)
                {
                    lblSummary.Text = "Bạn chưa có phòng nào đang thuê";
                    ShowEmpty("Bạn chưa có phòng nào đang thuê.\\n\\nHãy đăng ký thuê phòng tại menu 'Tìm phòng trống'.");
                    return;
                }

                HideEmpty();
                dgvRooms.DataSource = contracts;

                lblSummary.Text = $"Tổng: {contracts.Count} phòng đang thuê";

                // Format
                foreach (DataGridViewColumn col in dgvRooms.Columns)
                {
                    if (col.Name == "GiaThue")
                        col.DefaultCellStyle.Format = "N0";
                    if (col.Name is "NgayBatDau" or "NgayKetThuc")
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowEmpty(string message)
        {
            lblEmptyMessage.Text = message;
            lblEmptyMessage.Visible = true;
            dgvRooms.Visible = false;
        }

        private void HideEmpty()
        {
            lblEmptyMessage.Visible = false;
            dgvRooms.Visible = true;
            dgvRooms.BringToFront();
        }

        private void DgvRooms_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var contract = dgvRooms.Rows[e.RowIndex].DataBoundItem as HopDong;
            if (contract == null) return;

            var columnName = dgvRooms.Columns[e.ColumnIndex].Name;

            if (columnName == "btnDetail")
            {
                ShowRoomDetail(contract);
            }
        }

        private void DgvRooms_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var contract = dgvRooms.Rows[e.RowIndex].DataBoundItem as HopDong;
            if (contract == null) return;

            // Tùy chỉnh hiển thị trạng thái hợp đồng
            if (dgvRooms.Columns[e.ColumnIndex].Name == "TrangThaiHopDong")
            {
                var cell = dgvRooms.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

                // Kiểm tra hết hạn
                if (contract.TrangThai == "Active" && contract.NgayKetThuc < DateTime.Now)
                {
                    e.Value = "Đã hết hạn";
                    cell.Style.ForeColor = Color.FromArgb(231, 76, 60); // Red
                }
                else if (contract.TrangThai == "Active")
                {
                    e.Value = "Đang hiệu lực";
                    cell.Style.ForeColor = Color.FromArgb(39, 174, 96); // Green
                }
                else
                {
                    e.Value = contract.TrangThai;
                    cell.Style.ForeColor = Color.FromArgb(149, 165, 166); // Gray
                }
            }
        }

        private async void ShowRoomDetail(HopDong contract)
        {
            try
            {
                var room = await _phongRepo.GetByMaPhongAsync(contract.MaPhong);
                if (room == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin phòng", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var assets = (await _taiSanRepo.GetByPhongIdAsync(room.PhongId)).ToList();

                // Kiểm tra trạng thái hợp đồng
                var contractStatusDisplay = "Đang hiệu lực";
                if (contract.TrangThai == "Active" && contract.NgayKetThuc < DateTime.Now)
                {
                    contractStatusDisplay = "Đã hết hạn";
                }

                var detail = $"=== THÔNG TIN PHÒNG ===\\n" +
                            $"Mã phòng: {room.MaPhong}\\n" +
                            $"Tòa nhà: {room.BuildingName}\\n" +
                            $"Loại phòng: {room.TenLoai}\\n" +
                            $"Tầng: {room.Tang}\\n" +
                            $"Diện tích: {room.DienTich} m²\\n" +
                            $"Giá thuê: {room.GiaThue:N0} VNĐ/tháng\\n" +
                            $"Số người tối đa: {room.SoNguoiToiDa}\\n\\n" +
                            $"=== THÔNG TIN HỢP ĐỒNG ===\\n" +
                            $"Mã hợp đồng: {contract.MaHopDong}\\n" +
                            $"Ngày bắt đầu: {contract.NgayBatDau:dd/MM/yyyy}\\n" +
                            $"Ngày kết thúc: {contract.NgayKetThuc:dd/MM/yyyy}\\n" +
                            $"Giá thuê: {contract.GiaThue:N0} VNĐ/tháng\\n" +
                            $"Tiền cọc: {contract.TienCoc:N0} VNĐ\\n" +
                            $"Chu kỳ thanh toán: {contract.ChuKyThanhToan} tháng\\n" +
                            $"Trạng thái: {contractStatusDisplay}\\n";

                if (assets.Count > 0)
                {
                    detail += $"\\n=== TÀI SẢN TRONG PHÒNG ({assets.Count}) ===\\n";
                    foreach (var asset in assets)
                    {
                        detail += $"• {asset.TenTaiSan} - SL: {asset.SoLuong} - Tình trạng: {asset.TinhTrang}\\n";
                        if (!string.IsNullOrEmpty(asset.GhiChu))
                            detail += $"  Ghi chú: {asset.GhiChu}\\n";
                    }
                }

                MessageBox.Show(detail, "Chi tiết phòng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyRoom";
            this.Size = new Size(1100, 700);
        }
    }
}
