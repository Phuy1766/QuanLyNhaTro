using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyInvoice : UserControl
    {
        private readonly HoaDonRepository _hoaDonRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _tenantUserId;

        private ModernDataGrid dgvInvoices = null!;
        private Label lblSummary = null!;
        private Label lblEmptyMessage = null!;
        private Panel pnlMainCard;

        public ucMyInvoice(int tenantUserId)
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
                Text = "Hóa đơn",
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(0, 15),
                AutoSize = true
            };
            var lblTitle = new Label
            {
                Text = "Hóa đơn của tôi",
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
            dgvInvoices = new ModernDataGrid
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
                    SelectionBackColor = Color.FromArgb(0, 122, 255),   // ĐÃ SỬA
                    SelectionForeColor = Color.White
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(248, 249, 250)
                }
            };

            // Hover row
            dgvInvoices.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvInvoices.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
            };
            dgvInvoices.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvInvoices.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 249, 250);
            };

            // Cột
            UIHelper.AddColumn(dgvInvoices, "MaHoaDon", "Mã hóa đơn", "MaHoaDon", 100);
            UIHelper.AddColumn(dgvInvoices, "ThangNam", "Tháng", "ThangNam", 110);
            UIHelper.AddColumn(dgvInvoices, "TongCong", "Tổng tiền", "TongCong", 140);
            UIHelper.AddColumn(dgvInvoices, "DaThanhToan", "Đã TT", "DaThanhToan", 130);
            UIHelper.AddColumn(dgvInvoices, "ConNo", "Còn nợ", "ConNo", 130);
            UIHelper.AddColumn(dgvInvoices, "TrangThai", "Trạng thái", "TrangThai", 130);
            UIHelper.AddColumn(dgvInvoices, "NgayHetHan", "Hạn TT", "NgayHetHan", 140);

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
            dgvInvoices.Columns.Add(btnDetail);

            var btnPayment = new DataGridViewButtonColumn
            {
                Name = "btnPayment",
                HeaderText = "Thanh toán",
                Text = "Thanh toán",
                UseColumnTextForButtonValue = false,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 167, 69),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                }
            };
            dgvInvoices.Columns.Add(btnPayment);
            dgvInvoices.CellClick += DgvInvoices_CellClick;
            dgvInvoices.CellFormatting += DgvInvoices_CellFormatting;

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
            pnlMainCard.Controls.AddRange(new Control[] { dgvInvoices, lblEmptyMessage, pnlSummary, pnlHeader });
            this.Controls.Add(pnlMainCard);
        }

        private async void LoadDataAsync()
        {
            try
            {
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_tenantUserId);
                if (contract == null)
                {
                    lblSummary.Text = "Bạn chưa có hợp đồng thuê phòng";
                    ShowEmpty("Bạn chưa có hợp đồng thuê phòng.\n\nHãy đăng ký thuê phòng tại menu 'Tìm phòng trống'.");
                    return;
                }

                var list = (await _hoaDonRepo.GetByContractAsync(contract.MaHopDong)).ToList();

                if (list.Count == 0)
                {
                    lblSummary.Text = "Chưa có hóa đơn nào";
                    ShowEmpty("Chưa có hóa đơn nào được tạo.\n\nHóa đơn sẽ được tạo tự động hàng tháng.");
                    return;
                }

                HideEmpty();
                dgvInvoices.DataSource = list;

                var tongNo = list.Sum(x => x.ConNo);
                var chuaTT = list.Count(x => x.TrangThai != "DaThanhToan");
                lblSummary.Text = $"Tổng: {list.Count} hóa đơn • Chưa thanh toán: {chuaTT} • Tổng công nợ: {tongNo:N0} VNĐ";

                // Format
                foreach (DataGridViewColumn col in dgvInvoices.Columns)
                {
                    if (col.Name is "TongCong" or "DaThanhToan" or "ConNo")
                        col.DefaultCellStyle.Format = "N0";
                    if (col.Name is "ThangNam")
                        col.DefaultCellStyle.Format = "MM/yyyy";
                    if (col.Name is "NgayHetHan")
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                // Tô màu trạng thái
                foreach (DataGridViewRow row in dgvInvoices.Rows)
                {
                    var status = row.Cells["TrangThai"].Value?.ToString();
                    var cell = row.Cells["TrangThai"];
                    cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

                    // Cập nhật hiển thị trạng thái
                    cell.Value = GetTrangThaiDisplay(status ?? "");

                    cell.Style.ForeColor = status switch
                    {
                        "DaThanhToan" => Color.FromArgb(39, 174, 96),
                        "ChoXacNhan" => Color.FromArgb(255, 193, 7),
                        "QuaHan" => Color.FromArgb(231, 76, 60),
                        _ => Color.FromArgb(230, 126, 34)
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 2 HÀM BẮT BUỘC PHẢI CÓ – ĐÃ SỬA TÊN ĐÚNG
        private void ShowEmpty(string message)
        {
            lblEmptyMessage.Text = message;
            lblEmptyMessage.Visible = true;
            dgvInvoices.Visible = false;
        }

        private void HideEmpty()
        {
            lblEmptyMessage.Visible = false;
            dgvInvoices.Visible = true;
            dgvInvoices.BringToFront();
        }

        private void DgvInvoices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var hd = dgvInvoices.Rows[e.RowIndex].DataBoundItem as HoaDon;
            if (hd == null) return;

            var columnName = dgvInvoices.Columns[e.ColumnIndex].Name;

            if (columnName == "btnDetail")
            {
                ShowInvoiceDetail(hd);
            }
            else if (columnName == "btnPayment")
            {
                // Chỉ cho phép thanh toán nếu còn nợ và chưa thanh toán
                if (hd.TrangThai == "DaThanhToan")
                {
                    MessageBox.Show("Hóa đơn này đã được thanh toán đầy đủ.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (hd.TrangThai == "ChoXacNhan")
                {
                    MessageBox.Show("Hóa đơn này đang chờ quản lý xác nhận thanh toán.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (hd.ConNo <= 0)
                {
                    MessageBox.Show("Hóa đơn này không còn nợ.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowPaymentDialog(hd);
            }
        }

        private void DgvInvoices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var hd = dgvInvoices.Rows[e.RowIndex].DataBoundItem as HoaDon;
            if (hd == null) return;

            // Tùy chỉnh nút thanh toán
            if (dgvInvoices.Columns[e.ColumnIndex].Name == "btnPayment")
            {
                var cell = dgvInvoices.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    if (hd.TrangThai == "DaThanhToan")
                    {
                        cell.Value = "Đã thanh toán";
                        cell.Style.BackColor = Color.FromArgb(108, 117, 125); // Gray
                    }
                    else if (hd.TrangThai == "ChoXacNhan")
                    {
                        cell.Value = "Chờ xác nhận";
                        cell.Style.BackColor = Color.FromArgb(255, 193, 7); // Yellow/Orange
                    }
                    else if (hd.ConNo > 0)
                    {
                        cell.Value = "Thanh toán";
                        cell.Style.BackColor = Color.FromArgb(40, 167, 69); // Green
                    }
                }
            }
        }

        private void ShowInvoiceDetail(HoaDon hd)
        {
            var detail = $"Hóa đơn #{hd.MaHoaDon}\n" +
                        $"Tháng: {hd.ThangNam:MM/yyyy}\n" +
                        $"Phòng: {hd.MaPhong}\n\n" +
                        $"Tiền phòng: {hd.GiaPhong:N0} VNĐ\n" +
                        $"Tiền điện: {hd.TienDien:N0} VNĐ\n" +
                        $"Tiền nước: {hd.TienNuoc:N0} VNĐ\n" +
                        $"Dịch vụ khác: {hd.TienDichVu:N0} VNĐ\n\n" +
                        $"Tổng cộng: {hd.TongCong:N0} VNĐ\n" +
                        $"Đã thanh toán: {hd.DaThanhToan:N0} VNĐ\n" +
                        $"Còn nợ: {hd.ConNo:N0} VNĐ\n\n" +
                        $"Trạng thái: {GetTrangThaiDisplay(hd.TrangThai)}\n" +
                        $"Hạn thanh toán: {hd.NgayHetHan:dd/MM/yyyy}";

            MessageBox.Show(detail, "Chi tiết hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowPaymentDialog(HoaDon hd)
        {
            var dialog = new Forms.InvoicePaymentDialog(hd, _tenantUserId);
            var result = dialog.ShowDialog();

            if (result == DialogResult.OK && dialog.PaymentConfirmed)
            {
                // Reload data sau khi thanh toán
                LoadDataAsync();
            }
        }

        private string GetTrangThaiDisplay(string trangThai)
        {
            return trangThai switch
            {
                "DaThanhToan" => "Đã thanh toán",
                "ChuaThanhToan" => "Chưa thanh toán",
                "ChoXacNhan" => "Chờ xác nhận",
                "QuaHan" => "Quá hạn",
                _ => trangThai
            };
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1100, 700);
            this.Name = "ucMyInvoice";
        }
    }
}