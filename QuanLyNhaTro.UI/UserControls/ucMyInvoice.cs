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
        private Label lblEmptyMessage = null!;
        private Panel pnlMainCard = null!;

        public ucMyInvoice(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            BuildModernUI();
            LoadDataAsync();
        }

        private void BuildModernUI()
        {
            this.BackColor = Color.FromArgb(247, 249, 252);
            this.Padding = new Padding(24);

            // Container chính
            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // ===== TIÊU ĐỀ TRANG =====
            var pnlTitleSection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 12)
            };

            var lblIcon = new Label
            {
                Text = "💰",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 136, 229),
                Location = new Point(0, 8),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Hóa đơn của tôi",
                Font = new Font("Segoe UI Semibold", 24F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(45, 6),
                AutoSize = true
            };

            pnlTitleSection.Controls.AddRange(new Control[] { lblIcon, lblTitle });

            // ===== INFO SUMMARY CARDS - RESPONSIVE GRID =====
            var pnlCardsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 20),
                Margin = new Padding(0)
            };

            // Thiết lập các cột co giãn đều - QUAN TRỌNG: SizeType.Percent
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Card 1: Tổng số hóa đơn
            var card1 = CreateInfoCard("📋", "Tổng hóa đơn", "0", Color.FromArgb(30, 136, 229));
            card1.Tag = "total_invoices";
            card1.Margin = new Padding(0, 0, 8, 0);
            card1.Dock = DockStyle.Fill;

            // Card 2: Chưa thanh toán
            var card2 = CreateInfoCard("⏳", "Chưa thanh toán", "0", Color.FromArgb(255, 193, 7));
            card2.Tag = "unpaid_count";
            card2.Margin = new Padding(4, 0, 4, 0);
            card2.Dock = DockStyle.Fill;

            // Card 3: Tổng công nợ
            var card3 = CreateInfoCard("💳", "Tổng công nợ", "0đ", Color.FromArgb(220, 53, 69));
            card3.Tag = "total_debt";
            card3.Margin = new Padding(4, 0, 4, 0);
            card3.Dock = DockStyle.Fill;

            // Card 4: Sắp đến hạn
            var card4 = CreateInfoCard("⚠", "Sắp đến hạn", "0", Color.FromArgb(255, 152, 0));
            card4.Tag = "upcoming";
            card4.Margin = new Padding(8, 0, 0, 0);
            card4.Dock = DockStyle.Fill;

            pnlCardsContainer.Controls.Add(card1, 0, 0);
            pnlCardsContainer.Controls.Add(card2, 1, 0);
            pnlCardsContainer.Controls.Add(card3, 2, 0);
            pnlCardsContainer.Controls.Add(card4, 3, 0);

            // ===== TIÊU ĐỀ BẢNG =====
            var pnlTableTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 8)
            };

            var lblTableTitle = new Label
            {
                Text = "Danh sách hóa đơn của tôi",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTableTitle.Controls.Add(lblTableTitle);

            // ===== BẢNG DỮ LIỆU - TÁCH RIÊNG, KHÔNG TRONG CARD =====
            pnlMainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle
            };

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
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 48 },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 136, 229),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 10.5F),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(10, 0, 10, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(12, 8, 12, 8),
                    Font = new Font("Segoe UI", 10F),
                    SelectionBackColor = Color.FromArgb(30, 136, 229),
                    SelectionForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
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
                    dgvInvoices.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(225, 242, 255);
            };
            dgvInvoices.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvInvoices.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 249, 250);
            };

            // Cột
            UIHelper.AddColumn(dgvInvoices, "MaHoaDon", "Mã HĐ", "MaHoaDon", 100);
            UIHelper.AddColumn(dgvInvoices, "ThangNam", "Tháng", "ThangNam", 100);
            UIHelper.AddColumn(dgvInvoices, "TongCong", "Tổng tiền", "TongCong", 130);
            UIHelper.AddColumn(dgvInvoices, "DaThanhToan", "Đã TT", "DaThanhToan", 120);
            UIHelper.AddColumn(dgvInvoices, "ConNo", "Còn nợ", "ConNo", 120);
            UIHelper.AddColumn(dgvInvoices, "TrangThai", "Trạng thái", "TrangThai", 130);
            UIHelper.AddColumn(dgvInvoices, "NgayHetHan", "Hạn TT", "NgayHetHan", 120);

            var btnDetail = new DataGridViewButtonColumn
            {
                Name = "btnDetail",
                HeaderText = "Chi tiết",
                Text = "Xem",
                UseColumnTextForButtonValue = true,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 136, 229),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Padding = new Padding(8, 4, 8, 4)
                }
            };
            dgvInvoices.Columns.Add(btnDetail);

            var btnPayment = new DataGridViewButtonColumn
            {
                Name = "btnPayment",
                HeaderText = "Thanh toán",
                Text = "Thanh toán",
                UseColumnTextForButtonValue = false,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 167, 69),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Padding = new Padding(8, 4, 8, 4)
                }
            };
            dgvInvoices.Columns.Add(btnPayment);
            dgvInvoices.CellClick += DgvInvoices_CellClick;
            dgvInvoices.CellFormatting += DgvInvoices_CellFormatting;

            // Empty message
            lblEmptyMessage = new Label
            {
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(149, 165, 166),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };

            // Layout
            pnlMainCard.Controls.AddRange(new Control[] { dgvInvoices, lblEmptyMessage });

            // Thêm các controls theo thứ tự dock (bottom to top)
            pnlContainer.Controls.Add(pnlMainCard);        // Dock.Fill - chiếm phần còn lại
            pnlContainer.Controls.Add(pnlTableTitle);      // Dock.Top
            pnlContainer.Controls.Add(pnlCardsContainer);  // Dock.Top
            pnlContainer.Controls.Add(pnlTitleSection);    // Dock.Top

            this.Controls.Add(pnlContainer);
        }

        private Panel CreateInfoCard(string icon, string title, string value, Color accentColor)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                MinimumSize = new Size(200, 84),
                Height = 84
            };
            UIHelper.ApplyCardShadow(card);
            UIHelper.RoundControl(card, 10);

            // Icon
            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 28F),
                ForeColor = accentColor,
                Location = new Point(16, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Title
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(75, 22),
                AutoSize = true
            };

            // Value
            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI Semibold", 18F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(75, 40),
                AutoSize = true,
                Tag = "value"
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblValue });
            return card;
        }

        private void UpdateInfoCards(int totalInvoices, int unpaidCount, decimal totalDebt, int upcomingCount)
        {
            var container = this.Controls[0];
            foreach (Control ctrl in container.Controls)
            {
                if (ctrl is TableLayoutPanel tlp)
                {
                    foreach (Control childCtrl in tlp.Controls)
                    {
                        if (childCtrl is Panel panel && panel.Controls.OfType<Label>().Any(l => l.Tag?.ToString() == "value"))
                        {
                            var valueLabel = panel.Controls.OfType<Label>().First(l => l.Tag?.ToString() == "value");

                            switch (panel.Tag?.ToString())
                            {
                                case "total_invoices":
                                    valueLabel.Text = totalInvoices.ToString();
                                    break;
                                case "unpaid_count":
                                    valueLabel.Text = unpaidCount.ToString();
                                    break;
                                case "total_debt":
                                    valueLabel.Text = $"{totalDebt:N0}đ";
                                    valueLabel.Font = new Font("Segoe UI Semibold", 14F);
                                    break;
                                case "upcoming":
                                    valueLabel.Text = upcomingCount.ToString();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_tenantUserId);
                if (contract == null)
                {
                    ShowEmpty("Bạn chưa có hợp đồng thuê phòng.\n\nHãy đăng ký thuê phòng tại menu 'Tìm phòng trống'.");
                    UpdateInfoCards(0, 0, 0, 0);
                    return;
                }

                var list = (await _hoaDonRepo.GetByContractAsync(contract.MaHopDong)).ToList();

                if (list.Count == 0)
                {
                    ShowEmpty("Chưa có hóa đơn nào được tạo.\n\nHóa đơn sẽ được tạo tự động hàng tháng.");
                    UpdateInfoCards(0, 0, 0, 0);
                    return;
                }

                HideEmpty();
                dgvInvoices.DataSource = list;

                // Tính toán thống kê
                var tongNo = list.Sum(x => x.ConNo);
                var chuaTT = list.Count(x => x.TrangThai != "DaThanhToan");
                var sapDenHan = list.Count(x => x.TrangThai != "DaThanhToan" &&
                                               x.NgayHetHan >= DateTime.Now &&
                                               x.NgayHetHan <= DateTime.Now.AddDays(7));

                // Cập nhật Info Cards
                UpdateInfoCards(list.Count, chuaTT, tongNo, sapDenHan);

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
            try
            {
                var dialog = new Forms.InvoiceDetailDialog(hd);
                dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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