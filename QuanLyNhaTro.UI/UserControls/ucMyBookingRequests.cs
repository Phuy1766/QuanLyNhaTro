using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyBookingRequests : UserControl
    {
        private readonly YeuCauThuePhongRepository _requestRepo = new();
        private readonly PaymentRepository _paymentRepo = new();
        private readonly int _currentTenantId;

        private ModernDataGrid dgvRequests = null!;
        private Label lblEmptyMessage = null!;
        private Panel pnlMainCard = null!;

        public ucMyBookingRequests(int userId)
        {
            _currentTenantId = userId;
            InitializeComponent();
            BuildModernUI();
            LoadRequestsAsync();
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
                Text = "📋",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 136, 229),
                Location = new Point(0, 8),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Yêu cầu thuê của tôi",
                Font = new Font("Segoe UI Semibold", 24F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(45, 6),
                AutoSize = true
            };

            pnlTitleSection.Controls.AddRange(new Control[] { lblIcon, lblTitle });

            // ===== INFO SUMMARY CARDS =====
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

            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var card1 = CreateInfoCard("📊", "Tổng yêu cầu", "0", Color.FromArgb(30, 136, 229));
            card1.Tag = "total";
            card1.Margin = new Padding(0, 0, 8, 0);
            card1.Dock = DockStyle.Fill;

            var card2 = CreateInfoCard("⏳", "Chờ xử lý", "0", Color.FromArgb(255, 193, 7));
            card2.Tag = "pending";
            card2.Margin = new Padding(4, 0, 4, 0);
            card2.Dock = DockStyle.Fill;

            var card3 = CreateInfoCard("✓", "Đã duyệt", "0", Color.FromArgb(40, 167, 69));
            card3.Tag = "approved";
            card3.Margin = new Padding(4, 0, 4, 0);
            card3.Dock = DockStyle.Fill;

            var card4 = CreateInfoCard("✗", "Từ chối", "0", Color.FromArgb(220, 53, 69));
            card4.Tag = "rejected";
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
                Text = "Danh sách yêu cầu thuê phòng",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTableTitle.Controls.Add(lblTableTitle);

            // ===== BẢNG DỮ LIỆU =====
            pnlMainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle
            };

            dgvRequests = new ModernDataGrid
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

            // Hover effect
            dgvRequests.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvRequests.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(225, 242, 255);
            };
            dgvRequests.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvRequests.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 249, 250);
            };

            // Columns
            UIHelper.AddColumn(dgvRequests, "MaYeuCau", "Mã YC", "MaYeuCau", 80);
            UIHelper.AddColumn(dgvRequests, "NgayGui", "Ngày gửi", "NgayGui", 120);
            UIHelper.AddColumn(dgvRequests, "MaPhong", "Phòng", "MaPhong", 100);
            UIHelper.AddColumn(dgvRequests, "GiaPhong", "Giá thuê (VNĐ)", "GiaPhong", 140);
            UIHelper.AddColumn(dgvRequests, "TrangThai", "Trạng thái", "TrangThai", 150);

            var btnDetail = new DataGridViewButtonColumn
            {
                Name = "btnDetail",
                HeaderText = "Chi tiết",
                Text = "Xem",
                UseColumnTextForButtonValue = true,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 136, 229),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Padding = new Padding(8, 4, 8, 4)
                }
            };
            dgvRequests.Columns.Add(btnDetail);

            dgvRequests.CellClick += DgvRequests_CellClick;
            dgvRequests.CellFormatting += DgvRequests_CellFormatting;

            // Empty message
            lblEmptyMessage = new Label
            {
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(149, 165, 166),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlMainCard.Controls.AddRange(new Control[] { dgvRequests, lblEmptyMessage });

            // Layout
            pnlContainer.Controls.Add(pnlMainCard);
            pnlContainer.Controls.Add(pnlTableTitle);
            pnlContainer.Controls.Add(pnlCardsContainer);
            pnlContainer.Controls.Add(pnlTitleSection);

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

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 28F),
                ForeColor = accentColor,
                Location = new Point(16, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(75, 22),
                AutoSize = true
            };

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

        private void UpdateInfoCards(int total, int pending, int approved, int rejected)
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
                                case "total":
                                    valueLabel.Text = total.ToString();
                                    break;
                                case "pending":
                                    valueLabel.Text = pending.ToString();
                                    break;
                                case "approved":
                                    valueLabel.Text = approved.ToString();
                                    break;
                                case "rejected":
                                    valueLabel.Text = rejected.ToString();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async void LoadRequestsAsync()
        {
            try
            {
                var requests = (await _requestRepo.GetByTenantAsync(_currentTenantId))
                    .OrderByDescending(r => r.NgayGui)
                    .ToList();

                if (requests.Count == 0)
                {
                    ShowEmpty("Bạn chưa có yêu cầu thuê phòng nào.\n\nHãy tìm phòng trống và gửi yêu cầu thuê.");
                    UpdateInfoCards(0, 0, 0, 0);
                    return;
                }

                HideEmpty();
                dgvRequests.DataSource = requests;

                // Statistics
                var total = requests.Count;
                var pending = requests.Count(r => r.TrangThai == "Pending" || r.TrangThai == "PendingPayment" || r.TrangThai == "WaitingConfirm");
                var approved = requests.Count(r => r.TrangThai == "Approved");
                var rejected = requests.Count(r => r.TrangThai == "Rejected");

                UpdateInfoCards(total, pending, approved, rejected);

                // Format columns
                foreach (DataGridViewColumn col in dgvRequests.Columns)
                {
                    if (col.Name == "GiaPhong")
                        col.DefaultCellStyle.Format = "N0";
                    if (col.Name == "NgayGui")
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
            dgvRequests.Visible = false;
        }

        private void HideEmpty()
        {
            lblEmptyMessage.Visible = false;
            dgvRequests.Visible = true;
            dgvRequests.BringToFront();
        }

        private void DgvRequests_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var request = dgvRequests.Rows[e.RowIndex].DataBoundItem as YeuCauThuePhong;
            if (request == null) return;

            var columnName = dgvRequests.Columns[e.ColumnIndex].Name;

            if (columnName == "btnDetail")
            {
                ShowRequestDetail(request);
            }
        }

        private void DgvRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var request = dgvRequests.Rows[e.RowIndex].DataBoundItem as YeuCauThuePhong;
            if (request == null) return;

            if (dgvRequests.Columns[e.ColumnIndex].Name == "TrangThai")
            {
                var cell = dgvRequests.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

                switch (request.TrangThai)
                {
                    case "PendingPayment":
                        e.Value = "Chờ thanh toán";
                        cell.Style.ForeColor = Color.FromArgb(255, 193, 7);
                        break;
                    case "WaitingConfirm":
                        e.Value = "Chờ xác nhận";
                        cell.Style.ForeColor = Color.FromArgb(59, 130, 246);
                        break;
                    case "Pending":
                        e.Value = "Đang xử lý";
                        cell.Style.ForeColor = Color.FromArgb(139, 92, 246);
                        break;
                    case "Approved":
                        e.Value = "Đã duyệt";
                        cell.Style.ForeColor = Color.FromArgb(16, 185, 129);
                        break;
                    case "Rejected":
                        e.Value = "Từ chối";
                        cell.Style.ForeColor = Color.FromArgb(239, 68, 68);
                        break;
                    case "Canceled":
                        e.Value = "Đã hủy";
                        cell.Style.ForeColor = Color.FromArgb(107, 114, 128);
                        break;
                    default:
                        e.Value = request.TrangThai;
                        cell.Style.ForeColor = Color.FromArgb(107, 114, 128);
                        break;
                }
            }
        }

        private async void ShowRequestDetail(YeuCauThuePhong request)
        {
            try
            {
                // Pass null for payment info for now (can be implemented later if needed)
                var dialog = new Forms.BookingRequestDetailDialog(request, null);
                dialog.ShowDialog(this);

                // Reload after dialog closes in case status changed
                LoadRequestsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyBookingRequests";
            this.Size = new Size(1100, 700);
        }
    }
}
