using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyInvoice : UserControl
    {
        private readonly HoaDonRepository _hoaDonRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _tenantUserId;

        private DataGridView dgv = null!;
        private Label lblSummary = null!;

        public ucMyInvoice(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            CreateLayout();
            LoadDataAsync();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblTitle = new Label
            {
                Text = "Hóa đơn của tôi",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Summary Panel
            var pnlSummary = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = ThemeManager.Background,
                Padding = new Padding(20, 10, 20, 10)
            };

            lblSummary = new Label
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = ThemeManager.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            pnlSummary.Controls.Add(lblSummary);
            this.Controls.Add(pnlSummary);

            // Grid Panel
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(15)
            };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Surface,
                BorderStyle = BorderStyle.None
            };
            UIHelper.StyleDataGridView(dgv);

            UIHelper.AddColumn(dgv, "MaHoaDon", "Mã HĐ", "MaHoaDon", 80);
            UIHelper.AddColumn(dgv, "ThangNam", "Tháng", "ThangNam", 90);
            UIHelper.AddColumn(dgv, "SoDienCu", "Điện cũ", "SoDienCu", 70);
            UIHelper.AddColumn(dgv, "SoDienMoi", "Điện mới", "SoDienMoi", 70);
            UIHelper.AddColumn(dgv, "SoNuocCu", "Nước cũ", "SoNuocCu", 70);
            UIHelper.AddColumn(dgv, "SoNuocMoi", "Nước mới", "SoNuocMoi", 70);
            UIHelper.AddColumn(dgv, "TongCong", "Tổng tiền", "TongCong", 100);
            UIHelper.AddColumn(dgv, "DaThanhToan", "Đã TT", "DaThanhToan", 90);
            UIHelper.AddColumn(dgv, "ConNo", "Còn nợ", "ConNo", 90);
            UIHelper.AddColumn(dgv, "TrangThai", "Trạng thái", "TrangThai", 100);
            UIHelper.AddColumn(dgv, "NgayHetHan", "Hạn TT", "NgayHetHan", 90);

            // Button column
            var btnCol = new DataGridViewButtonColumn
            {
                Name = "btnDetail",
                HeaderText = "",
                Text = "Chi tiết",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            dgv.Columns.Add(btnCol);

            dgv.CellClick += Dgv_CellClick;
            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
        }

        private async void LoadDataAsync()
        {
            try
            {
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_tenantUserId);
                if (contract == null)
                {
                    lblSummary.Text = "Bạn chưa có hợp đồng thuê phòng";
                    return;
                }

                var hoaDons = await _hoaDonRepo.GetByContractAsync(contract.MaHopDong);
                var list = hoaDons.ToList();
                dgv.DataSource = list;

                // Summary
                var tongNo = list.Sum(h => h.ConNo);
                var chuaTT = list.Count(h => h.TrangThai != "DaThanhToan");
                lblSummary.Text = $"Tổng: {list.Count} hóa đơn | Chưa thanh toán: {chuaTT} | Tổng công nợ: {tongNo:N0} VNĐ";

                // Format
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Name == "TongCong" || col.Name == "DaThanhToan" || col.Name == "ConNo")
                        col.DefaultCellStyle.Format = "N0";
                    if (col.Name == "ThangNam" || col.Name == "NgayHetHan")
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                // Color status
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var status = row.Cells["TrangThai"].Value?.ToString();
                    if (status == "DaThanhToan")
                        row.Cells["TrangThai"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                    else if (status == "QuaHan")
                        row.Cells["TrangThai"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                    else
                        row.Cells["TrangThai"].Style.ForeColor = Color.FromArgb(245, 158, 11);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name == "btnDetail")
            {
                var hoaDon = dgv.Rows[e.RowIndex].DataBoundItem as HoaDon;
                if (hoaDon != null) ShowInvoiceDetail(hoaDon);
            }
        }

        private async void ShowInvoiceDetail(HoaDon hoaDon)
        {
            var form = new Form
            {
                Text = $"Chi tiết hóa đơn #{hoaDon.MaHoaDon}",
                Size = new Size(450, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25), BackColor = ThemeManager.Surface };
            form.Controls.Add(pnl);

            int y = 20;
            void AddRow(string label, string value, Color? valueColor = null)
            {
                pnl.Controls.Add(new Label
                {
                    Text = label,
                    Location = new Point(20, y),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = ThemeManager.TextSecondary,
                    AutoSize = true
                });
                pnl.Controls.Add(new Label
                {
                    Text = value,
                    Location = new Point(200, y),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = valueColor ?? ThemeManager.TextPrimary,
                    AutoSize = true
                });
                y += 35;
            }

            AddRow("Mã hóa đơn:", $"#{hoaDon.MaHoaDon}");
            AddRow("Tháng:", hoaDon.ThangNam.ToString("MM/yyyy"));
            AddRow("Phòng:", hoaDon.MaPhong);
            y += 10;

            // Separator
            pnl.Controls.Add(new Label { Text = "─────────────────────────────", Location = new Point(20, y), AutoSize = true, ForeColor = Color.Gray });
            y += 25;

            AddRow("Giá phòng:", $"{hoaDon.GiaPhong:N0} VNĐ");
            AddRow($"Điện ({hoaDon.SoDienMoi - hoaDon.SoDienCu} kWh):", $"{hoaDon.TienDien:N0} VNĐ");
            AddRow($"Nước ({hoaDon.SoNuocMoi - hoaDon.SoNuocCu} m³):", $"{hoaDon.TienNuoc:N0} VNĐ");
            AddRow("Dịch vụ khác:", $"{hoaDon.TienDichVu:N0} VNĐ");
            y += 10;

            pnl.Controls.Add(new Label { Text = "─────────────────────────────", Location = new Point(20, y), AutoSize = true, ForeColor = Color.Gray });
            y += 25;

            AddRow("TỔNG CỘNG:", $"{hoaDon.TongCong:N0} VNĐ", ThemeManager.Primary);
            AddRow("Đã thanh toán:", $"{hoaDon.DaThanhToan:N0} VNĐ", Color.FromArgb(16, 185, 129));
            AddRow("Còn nợ:", $"{hoaDon.ConNo:N0} VNĐ", hoaDon.ConNo > 0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(16, 185, 129));
            AddRow("Trạng thái:", hoaDon.TrangThai == "DaThanhToan" ? "Đã thanh toán" : hoaDon.TrangThai == "QuaHan" ? "Quá hạn" : "Chưa thanh toán");
            AddRow("Hạn thanh toán:", hoaDon.NgayHetHan?.ToString("dd/MM/yyyy") ?? "N/A");

            // Close button
            var btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 35),
                Location = new Point(170, y + 20),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => form.Close();
            pnl.Controls.Add(btnClose);

            form.ShowDialog();
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyInvoice";
            this.Size = new Size(1100, 700);
        }
    }
}
