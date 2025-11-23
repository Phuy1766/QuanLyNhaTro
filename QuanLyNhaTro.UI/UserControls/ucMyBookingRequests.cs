using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyBookingRequests : UserControl
    {
        private DataGridView dgvRequests;
        private Panel pnlDetail;
        private Panel pnlDetailContent;
        private Label lblNoSelection;
        private Panel pnlActions;
        private Button btnPayDeposit;
        private Button btnCancelRequest;
        private Button btnViewContract;

        private YeuCauThuePhongRepository _requestRepo;
        private PaymentRepository _paymentRepo;
        private int _currentTenantId;
        private YeuCauThuePhong _selectedRequest;

        public ucMyBookingRequests(int userId)
        {
            _currentTenantId = userId;
            _requestRepo = new YeuCauThuePhongRepository();
            _paymentRepo = new PaymentRepository();
            InitializeComponent();
            CreateLayout();
            LoadRequests(); // ✅ GỌI LOAD DATA
        }

        public void Initialize(int tenantId)
        {
            _currentTenantId = tenantId;
            LoadRequests();
        }

        private void CreateLayout()
        {
            this.Padding = new Padding(20);
            this.BackColor = ColorTranslator.FromHtml("#F3F4F6");

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 42F),
                    new ColumnStyle(SizeType.Percent, 58F)
                }
            };

            Panel pnlList = CreateListPanel();
            pnlDetail = CreateDetailPanel();

            mainLayout.Controls.Add(pnlList, 0, 0);
            mainLayout.Controls.Add(pnlDetail, 1, 0);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateListPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 10, 0)
            };

            Label lblTitle = new Label
            {
                Text = "Danh sách yêu cầu thuê",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1F2937"),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            dgvRequests = new DataGridView
            {
                Location = new Point(0, 35),
                Width = panel.Width,
                Height = panel.Height - 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 45 }
            };

            dgvRequests.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F9FAFB");
            dgvRequests.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#374151");
            dgvRequests.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvRequests.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvRequests.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);

            dgvRequests.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
            dgvRequests.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#1E40AF");
            dgvRequests.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvRequests.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);

            dgvRequests.SelectionChanged += DgvRequests_SelectionChanged;
            dgvRequests.CellFormatting += DgvRequests_CellFormatting;

            SetupListColumns();

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(dgvRequests);

            return panel;
        }

        private void SetupListColumns()
        {
            dgvRequests.Columns.Clear();

            dgvRequests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaYeuCau",
                HeaderText = "Mã YC",
                DataPropertyName = "MaYeuCau",
                FillWeight = 20
            });

            dgvRequests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NgayGui",
                HeaderText = "Ngày tạo",
                DataPropertyName = "NgayGui",
                FillWeight = 25,
                DefaultCellStyle = { Format = "dd/MM/yyyy" }
            });

            dgvRequests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaPhong",
                HeaderText = "Phòng",
                DataPropertyName = "MaPhong",
                FillWeight = 20
            });

            dgvRequests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "GiaPhong",
                HeaderText = "Giá thuê",
                DataPropertyName = "GiaPhong",
                FillWeight = 20,
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvRequests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = "Trạng thái",
                DataPropertyName = "TrangThai",
                FillWeight = 25
            });
        }

        private void DgvRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvRequests.Columns[e.ColumnIndex].Name == "TrangThai" && e.Value != null)
            {
                string status = e.Value.ToString();
                e.Value = GetStatusDisplayText(status);
                e.CellStyle.ForeColor = GetStatusColor(status);
            }
        }

        private string GetStatusDisplayText(string status)
        {
            switch (status)
            {
                case "PendingPayment": return "Chờ thanh toán";
                case "WaitingConfirm": return "Chờ xác nhận";
                case "PendingApprove": return "Chờ duyệt";
                case "Pending": return "Đang xử lý";
                case "Approved": return "Đã duyệt";
                case "Rejected": return "Từ chối";
                case "Canceled": return "Đã hủy";
                default: return status;
            }
        }

        private Color GetStatusColor(string status)
        {
            switch (status)
            {
                case "Approved": return ColorTranslator.FromHtml("#10B981");
                case "Rejected": return ColorTranslator.FromHtml("#EF4444");
                case "Canceled": return ColorTranslator.FromHtml("#6B7280");
                case "PendingPayment": return ColorTranslator.FromHtml("#F59E0B");
                case "WaitingConfirm": return ColorTranslator.FromHtml("#3B82F6");
                case "PendingApprove": return ColorTranslator.FromHtml("#8B5CF6");
                case "Pending": return ColorTranslator.FromHtml("#6366F1");
                default: return ColorTranslator.FromHtml("#6B7280");
            }
        }

        private Panel CreateDetailPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0)
            };

            Panel innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            RoundCorners(innerPanel, 12);

            lblNoSelection = new Label
            {
                Text = "Chọn một yêu cầu để xem chi tiết",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = ColorTranslator.FromHtml("#9CA3AF"),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            pnlDetailContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Visible = false,
                Padding = new Padding(30, 25, 30, 25)
            };

            pnlActions = CreateActionsPanel();

            innerPanel.Controls.Add(lblNoSelection);
            innerPanel.Controls.Add(pnlDetailContent);
            innerPanel.Controls.Add(pnlActions);

            panel.Controls.Add(innerPanel);

            return panel;
        }

        private Panel CreateActionsPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = ColorTranslator.FromHtml("#F9FAFC"),
                Padding = new Padding(30, 15, 30, 15),
                Visible = false
            };

            Panel lineTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ColorTranslator.FromHtml("#E5E7EB")
            };

            FlowLayoutPanel flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(0)
            };

            btnPayDeposit = CreateButton("💳 Thanh toán cọc QR", ColorTranslator.FromHtml("#10B981"));
            btnPayDeposit.Click += BtnPayDeposit_Click;
            btnPayDeposit.Visible = false;

            btnCancelRequest = CreateButton("✗ Hủy yêu cầu", ColorTranslator.FromHtml("#EF4444"));
            btnCancelRequest.Click += BtnCancelRequest_Click;
            btnCancelRequest.Visible = false;

            btnViewContract = CreateButton("📄 Xem hợp đồng", ColorTranslator.FromHtml("#3B82F6"));
            btnViewContract.Click += BtnViewContract_Click;
            btnViewContract.Visible = false;

            flowButtons.Controls.Add(btnPayDeposit);
            flowButtons.Controls.Add(btnCancelRequest);
            flowButtons.Controls.Add(btnViewContract);

            panel.Controls.Add(flowButtons);
            panel.Controls.Add(lineTop);

            return panel;
        }

        private Button CreateButton(string text, Color color)
        {
            Button btn = new Button
            {
                Text = text,
                Width = 180,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };

            btn.FlatAppearance.BorderSize = 0;
            RoundCorners(btn, 6);

            return btn;
        }

        private void RoundCorners(Control control, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddArc(new Rectangle(0, 0, radius, radius), 180, 90);
            path.AddArc(new Rectangle(control.Width - radius, 0, radius, radius), 270, 90);
            path.AddArc(new Rectangle(control.Width - radius, control.Height - radius, radius, radius), 0, 90);
            path.AddArc(new Rectangle(0, control.Height - radius, radius, radius), 90, 90);
            path.CloseFigure();
            control.Region = new Region(path);
        }

        private void DgvRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvRequests.SelectedRows.Count > 0)
            {
                var row = dgvRequests.SelectedRows[0];
                int maYeuCau = Convert.ToInt32(row.Cells["MaYeuCau"].Value);
                LoadDetailPanel(maYeuCau);
            }
            else
            {
                ShowNoSelection();
            }
        }

        private void ShowNoSelection()
        {
            lblNoSelection.Visible = true;
            pnlDetailContent.Visible = false;
            pnlActions.Visible = false;
            _selectedRequest = null;
        }

        private async void LoadRequests()
        {
            try
            {
                var requests = (await _requestRepo.GetByTenantAsync(_currentTenantId)).ToList();
                dgvRequests.DataSource = requests.OrderByDescending(r => r.NgayGui).ToList();

                if (dgvRequests.Rows.Count == 0)
                {
                    ShowNoSelection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách yêu cầu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadDetailPanel(int maYeuCau)
        {
            try
            {
                var allRequests = await _requestRepo.GetByTenantAsync(_currentTenantId);
                _selectedRequest = allRequests.FirstOrDefault(r => r.MaYeuCau == maYeuCau);

                if (_selectedRequest == null)
                {
                    ShowNoSelection();
                    return;
                }

                BookingRequestDTO paymentInfo = null;
                try
                {
                    var allPayments = await _paymentRepo.GetAllBookingRequestsAsync(null);
                    paymentInfo = allPayments.FirstOrDefault(p => p.MaYeuCau == maYeuCau);
                }
                catch { }

                pnlDetailContent.Controls.Clear();

                int yPos = 0;

                Label lblHeader = new Label
                {
                    Text = $"Chi tiết yêu cầu #{_selectedRequest.MaYeuCau}",
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = ColorTranslator.FromHtml("#1F2937"),
                    AutoSize = true,
                    Location = new Point(0, yPos)
                };
                pnlDetailContent.Controls.Add(lblHeader);
                yPos += 35;

                Label lblDate = new Label
                {
                    Text = $"Ngày gửi: {_selectedRequest.NgayGui:dd/MM/yyyy HH:mm}",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = ColorTranslator.FromHtml("#6B7280"),
                    AutoSize = true,
                    Location = new Point(0, yPos)
                };
                pnlDetailContent.Controls.Add(lblDate);
                yPos += 35;

                Panel block1 = CreateInfoBlock("Thông tin phòng đăng ký", new[]
                {
                    new InfoItem("Tòa nhà", _selectedRequest.TenToaNha ?? "N/A"),
                    new InfoItem("Phòng", _selectedRequest.MaPhong ?? "N/A"),
                    new InfoItem("Loại phòng", _selectedRequest.DienTich.HasValue && _selectedRequest.SoNguoiToiDa.HasValue ? $"{_selectedRequest.DienTich:N0} m² - Tối đa {_selectedRequest.SoNguoiToiDa} người" : "N/A"),
                    new InfoItem("Giá thuê", _selectedRequest.GiaPhong.HasValue ? _selectedRequest.GiaPhong.Value.ToString("N0") + " VNĐ/tháng" : "N/A"),
                    new InfoItem("Ngày dự kiến chuyển vào", _selectedRequest.NgayBatDauMongMuon.ToString("dd/MM/yyyy")),
                    new InfoItem("Số người ở", _selectedRequest.SoNguoi.ToString()),
                    new InfoItem("Số tháng cọc", "1 tháng")
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block1);
                yPos += block1.Height + 20;

                decimal soTienCoc = _selectedRequest.GiaPhong ?? 0;
                string trangThaiThanhToan = "Chưa thanh toán";
                DateTime? ngayThanhToan = null;

                if (paymentInfo != null)
                {
                    if (paymentInfo.SoTienCoc.HasValue)
                        soTienCoc = paymentInfo.SoTienCoc.Value;
                    trangThaiThanhToan = paymentInfo.TrangThaiThanhToanDisplay ?? "Chưa thanh toán";
                    ngayThanhToan = paymentInfo.NgayThanhToan;
                }

                Panel block2 = CreateInfoBlock("Thông tin thanh toán cọc", new[]
                {
                    new InfoItem("Số tiền cọc", soTienCoc.ToString("N0") + " VNĐ"),
                    new InfoItem("Hình thức thanh toán", "QR Bank / Chuyển khoản"),
                    new InfoItem("Nội dung chuyển khoản", $"NTPRO_{_selectedRequest.MaYeuCau}_{_selectedRequest.MaPhong}"),
                    new InfoItem("Thời gian thanh toán", ngayThanhToan.HasValue ? ngayThanhToan.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa thanh toán"),
                    new InfoItem("Trạng thái thanh toán", trangThaiThanhToan)
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block2);
                yPos += block2.Height + 20;

                Panel block3 = CreateInfoBlock("Trạng thái xử lý", new[]
                {
                    new InfoItem("Trạng thái", GetStatusDisplayText(_selectedRequest.TrangThai)),
                    new InfoItem("Ngày xử lý", _selectedRequest.NgayXuLy.HasValue ? _selectedRequest.NgayXuLy.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa xử lý"),
                    new InfoItem("Lý do từ chối", string.IsNullOrEmpty(_selectedRequest.LyDoTuChoi) ? "N/A" : _selectedRequest.LyDoTuChoi),
                    new InfoItem("Ghi chú", string.IsNullOrEmpty(_selectedRequest.GhiChu) ? "N/A" : _selectedRequest.GhiChu)
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block3);

                lblNoSelection.Visible = false;
                pnlDetailContent.Visible = true;

                UpdateButtonStates(paymentInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CreateInfoBlock(string title, InfoItem[] items, int x, int y)
        {
            // Tính width dựa trên parent container
            int blockWidth = pnlDetailContent.ClientSize.Width - pnlDetailContent.Padding.Left - pnlDetailContent.Padding.Right;
            if (blockWidth < 300) blockWidth = 500; // Minimum width
            
            Panel block = new Panel
            {
                Location = new Point(x, y),
                Width = blockWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ColorTranslator.FromHtml("#F9FAFB"),
                Padding = new Padding(20)
            };

            int blockY = 0;

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1F2937"),
                AutoSize = true,
                Location = new Point(0, blockY)
            };
            block.Controls.Add(lblTitle);
            blockY += 35;

            foreach (var item in items)
            {
                Label lblLabel = new Label
                {
                    Text = item.Label + ":",
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = ColorTranslator.FromHtml("#6B7280"),
                    AutoSize = false,
                    Width = 180,
                    Height = 22,
                    Location = new Point(0, blockY),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                Label lblValue = new Label
                {
                    Text = item.Value,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = ColorTranslator.FromHtml("#1F2937"),
                    AutoSize = false,
                    Width = blockWidth - 240, // 240 = 180 (label) + 20 (padding left) + 20 (padding right) + 20 (gap)
                    Height = 22,
                    Location = new Point(200, blockY),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                block.Controls.Add(lblLabel);
                block.Controls.Add(lblValue);
                blockY += 30;
            }

            block.Height = blockY + 20;
            RoundCorners(block, 8);

            return block;
        }

        private void UpdateButtonStates(BookingRequestDTO paymentInfo)
        {
            btnPayDeposit.Visible = false;
            btnPayDeposit.Enabled = false;
            btnCancelRequest.Visible = false;
            btnCancelRequest.Enabled = false;
            btnViewContract.Visible = false;
            btnViewContract.Enabled = false;

            if (_selectedRequest == null)
            {
                pnlActions.Visible = false;
                return;
            }

            string status = _selectedRequest.TrangThai;

            if (status == "Approved" || status == "Rejected" || status == "Canceled")
            {
                if (status == "Approved")
                {
                    btnViewContract.Visible = true;
                    btnViewContract.Enabled = true;
                    btnViewContract.BackColor = ColorTranslator.FromHtml("#3B82F6");
                    btnViewContract.Cursor = Cursors.Hand;
                    pnlActions.Visible = true;
                }
                else
                {
                    pnlActions.Visible = false;
                }
                return;
            }

            bool showActions = false;

            if (status == "PendingPayment" || (paymentInfo != null && paymentInfo.TrangThaiThanhToan == "Pending"))
            {
                btnPayDeposit.Visible = true;
                btnPayDeposit.Enabled = true;
                btnPayDeposit.BackColor = ColorTranslator.FromHtml("#10B981");
                btnPayDeposit.Cursor = Cursors.Hand;
                showActions = true;
            }

            if (status == "Pending" || status == "WaitingConfirm" || status == "PendingApprove")
            {
                btnCancelRequest.Visible = true;
                btnCancelRequest.Enabled = true;
                btnCancelRequest.BackColor = ColorTranslator.FromHtml("#EF4444");
                btnCancelRequest.Cursor = Cursors.Hand;
                showActions = true;
            }

            pnlActions.Visible = showActions;
        }

        private void BtnPayDeposit_Click(object sender, EventArgs e)
        {
            if (_selectedRequest == null) return;

            MessageBox.Show("Chức năng thanh toán cọc QR đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnCancelRequest_Click(object sender, EventArgs e)
        {
            if (_selectedRequest == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn hủy yêu cầu #{_selectedRequest.MaYeuCau}?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var cancelResult = await _requestRepo.RejectAsync(_selectedRequest.MaYeuCau, _currentTenantId, "Tenant tự hủy yêu cầu");

                    if (cancelResult.Item1)
                    {
                        MessageBox.Show("Đã hủy yêu cầu thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadRequests();
                        ShowNoSelection();
                    }
                    else
                    {
                        MessageBox.Show($"Lỗi khi hủy yêu cầu: {cancelResult.Item2}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi hủy yêu cầu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnViewContract_Click(object sender, EventArgs e)
        {
            if (_selectedRequest == null) return;

            MessageBox.Show("Chức năng xem hợp đồng đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private struct InfoItem
        {
            public string Label;
            public string Value;

            public InfoItem(string label, string value)
            {
                Label = label;
                Value = value;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucMyBookingRequests";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }
}
