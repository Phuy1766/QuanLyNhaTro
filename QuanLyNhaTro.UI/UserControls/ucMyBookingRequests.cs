using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    /// <summary>
    /// Trang xem lịch sử yêu cầu thuê phòng của Tenant
    /// Redesigned với Master-Detail layout
    /// </summary>
    public partial class ucMyBookingRequests : UserControl
    {
        private readonly YeuCauThuePhongRepository _repo = new();
        private readonly PaymentRepository _paymentRepo = new();
        private readonly int _userId;

        // Left Panel - List Controls
        private Panel pnlLeft = null!;
        private DataGridView dgvRequests = null!;
        private ComboBox cboTrangThai = null!;
        private Label lblStats = null!;

        // Right Panel - Detail Controls
        private Panel pnlRight = null!;
        private Panel pnlDetailContent = null!;
        private Label lblNoSelection = null!;

        // Detail Section Controls
        private Label lblDetailTitle = null!;
        private Label lblDetailStatus = null!;
        private Label lblDetailCreatedDate = null!;

        // Room Info Block
        private Label lblRoomBuilding = null!;
        private Label lblRoomCode = null!;
        private Label lblRoomType = null!;
        private Label lblRoomPrice = null!;
        private Label lblStartDate = null!;
        private Label lblNumPeople = null!;
        private Label lblDepositMonths = null!;

        // Payment Info Block
        private Label lblPaymentAmount = null!;
        private Label lblPaymentMethod = null!;
        private Label lblPaymentContent = null!;
        private Label lblPaymentDate = null!;
        private Label lblPaymentStatus = null!;

        // Status Info Block
        private Label lblRequestStatus = null!;
        private Label lblProcessDate = null!;
        private Label lblRejectReason = null!;
        private Label lblNote = null!;

        // Action Buttons
        private Button btnPayDeposit = null!;
        private Button btnCancelRequest = null!;
        private Button btnViewContract = null!;

        private YeuCauThuePhong? _selectedRequest = null;

        public ucMyBookingRequests(int userId)
        {
            _userId = userId;
            InitializeComponent();
            CreateLayout();
            // Responsive layout khi thay đổi kích thước
            this.Resize += (_, __) => ApplyResponsiveLayout();
            ApplyResponsiveLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;
            this.Padding = new Padding(20);

            // ===== HEADER PANEL =====
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };
            UIHelper.RoundControl(pnlHeader, 12);

            // Title
            var lblTitle = new Label
            {
                Text = "Yêu cầu thuê của tôi",
                Font = new Font("Segoe UI Semibold", 16),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Quản lý các yêu cầu thuê phòng của bạn và theo dõi trạng thái xử lý.",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(0, 30),
                AutoSize = true
            };

            lblStats = new Label
            {
                Text = "Tổng: 0 | Chờ duyệt: 0 | Đã duyệt: 0 | Từ chối: 0",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(0, 55),
                AutoSize = true
            };

            // Filter Row
            int filterY = 65;
            int filterX = 450;

            var lblStatus = new Label
            {
                Text = "Trạng thái:",
                Location = new Point(filterX, filterY + 7),
                AutoSize = true,
                ForeColor = ThemeManager.TextPrimary
            };
            filterX += 80;

            cboTrangThai = new ComboBox
            {
                Location = new Point(filterX, filterY),
                Size = new Size(180, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cboTrangThai.Items.AddRange(new object[] {
                "Tất cả",
                "Chờ thanh toán",
                "Chờ duyệt",
                "Đã duyệt",
                "Từ chối",
                "Đã hủy"
            });
            cboTrangThai.SelectedIndex = 0;
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData();
            filterX += 200;

            var btnRefresh = new Button
            {
                Text = "↻",
                Location = new Point(filterX, filterY - 2),
                Size = new Size(35, 35),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 14)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnRefresh, 8);
            btnRefresh.Click += (s, e) => LoadData();

            pnlHeader.Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle, lblStats, lblStatus, cboTrangThai, btnRefresh
            });

            // ===== MAIN CONTAINER (Split into Left and Right) =====
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // ===== LEFT PANEL - LIST (40%) =====
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = (int)(this.Width * 0.40),
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };
            UIHelper.RoundControl(pnlLeft, 12);

            var lblListTitle = new Label
            {
                Text = "Danh sách yêu cầu",
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 0, 0, 10)
            };

            dgvRequests = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Surface,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 40
            };
            UIHelper.StyleDataGridView(dgvRequests);
            SetupListColumns();
            dgvRequests.SelectionChanged += DgvRequests_SelectionChanged;
            dgvRequests.CellFormatting += DgvRequests_CellFormatting;

            pnlLeft.Controls.Add(dgvRequests);
            pnlLeft.Controls.Add(lblListTitle);

            // ===== RIGHT PANEL - DETAIL (60%) =====
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };
            UIHelper.RoundControl(pnlRight, 12);

            CreateDetailPanel();

            // Add spacer between left and right
            var spacer = new Panel
            {
                Dock = DockStyle.Left,
                Width = 20,
                BackColor = Color.Transparent
            };

            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(spacer);
            pnlMain.Controls.Add(pnlLeft);

            // Add spacer between header and main
            var spacerTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20,
                BackColor = Color.Transparent
            };

            this.Controls.Add(pnlMain);
            this.Controls.Add(spacerTop);
            this.Controls.Add(pnlHeader);
        }


        /// <summary>
        /// Căn lại độ rộng panel danh sách (trái) ~40% màn hình,
        /// có giới hạn min/max để không quá nhỏ hoặc quá to.
        /// </summary>
        private void ApplyResponsiveLayout()
        {
            if (pnlLeft == null || pnlRight == null) return;

            int totalWidth = this.ClientSize.Width - this.Padding.Left - this.Padding.Right;
            if (totalWidth <= 0) return;

            int leftWidth = (int)(totalWidth * 0.4);

            int minLeft = 380;
            int maxLeft = 600;
            if (leftWidth < minLeft) leftWidth = minLeft;
            if (leftWidth > maxLeft) leftWidth = maxLeft;

            pnlLeft.Width = leftWidth;
        }


        private void CreateDetailPanel()
        {
            // No Selection Message
            lblNoSelection = new Label
            {
                Text = "Chọn một yêu cầu để xem chi tiết\n\nBạn chưa có yêu cầu nào? Hãy vào 'Tìm phòng trống' để bắt đầu.",
                Font = new Font("Segoe UI", 11),
                ForeColor = ThemeManager.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Detail Content Panel (hidden initially)
            pnlDetailContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Surface,
                Visible = false,
                AutoScroll = true
            };

            // Header
            lblDetailTitle = new Label
            {
                Text = "Chi tiết yêu cầu #0000",
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(0, 0),
                Size = new Size(400, 30)
            };

            lblDetailStatus = new Label
            {
                Text = "Chờ duyệt",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeManager.Warning,
                Location = new Point(410, 5),
                Size = new Size(120, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            UIHelper.RoundControl(lblDetailStatus, 6);

            lblDetailCreatedDate = new Label
            {
                Text = "Ngày gửi: 01/01/2025",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(0, 35),
                AutoSize = true
            };

            int y = 65;

            // ===== ROOM INFO BLOCK =====
            var roomLabels = CreateInfoBlock(pnlDetailContent, "Thông tin phòng đăng ký", y,
                new[] { "Tòa nhà:", "Mã phòng:", "Loại phòng:", "Giá thuê/tháng:", "Ngày dự kiến thuê:", "Số người:", "Số tháng cọc:" });
            lblRoomBuilding = roomLabels[0];
            lblRoomCode = roomLabels[1];
            lblRoomType = roomLabels[2];
            lblRoomPrice = roomLabels[3];
            lblStartDate = roomLabels[4];
            lblNumPeople = roomLabels[5];
            lblDepositMonths = roomLabels[6];
            y = roomLabels[^1].Top + 25;

            y += 20;

            // ===== PAYMENT INFO BLOCK =====
            var paymentLabels = CreateInfoBlock(pnlDetailContent, "Thông tin thanh toán cọc", y,
                new[] { "Số tiền cọc:", "Hình thức:", "Nội dung CK:", "Thời gian TT:", "Trạng thái TT:" });
            lblPaymentAmount = paymentLabels[0];
            lblPaymentMethod = paymentLabels[1];
            lblPaymentContent = paymentLabels[2];
            lblPaymentDate = paymentLabels[3];
            lblPaymentStatus = paymentLabels[4];
            y = paymentLabels[^1].Top + 25;

            y += 20;

            // ===== STATUS INFO BLOCK =====
            var statusLabels = CreateInfoBlock(pnlDetailContent, "Trạng thái xử lý", y,
                new[] { "Trạng thái:", "Ngày xử lý:", "Lý do từ chối:", "Ghi chú:" });
            lblRequestStatus = statusLabels[0];
            lblProcessDate = statusLabels[1];
            lblRejectReason = statusLabels[2];
            lblNote = statusLabels[3];
            y = statusLabels[^1].Top + 25;

            y += 30;

            // ===== ACTION BUTTONS =====
            var pnlActions = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(650, 50),
                BackColor = Color.Transparent
            };

            int btnX = 0;

            btnPayDeposit = CreateActionButton("💳 Thanh toán cọc QR", btnX, ThemeManager.Success);
            btnPayDeposit.Width = 180;
            btnPayDeposit.Click += BtnPayDeposit_Click;
            btnX += 190;

            btnCancelRequest = CreateActionButton("✗ Hủy yêu cầu", btnX, ThemeManager.Error);
            btnCancelRequest.Width = 130;
            btnCancelRequest.Click += BtnCancelRequest_Click;
            btnX += 140;

            btnViewContract = CreateActionButton("📄 Xem hợp đồng", btnX, ThemeManager.Primary);
            btnViewContract.Width = 150;
            btnViewContract.Click += BtnViewContract_Click;

            pnlActions.Controls.AddRange(new Control[] {
                btnPayDeposit, btnCancelRequest, btnViewContract
            });

            pnlDetailContent.Controls.AddRange(new Control[] {
                lblDetailTitle, lblDetailStatus, lblDetailCreatedDate, pnlActions
            });

            pnlRight.Controls.Add(pnlDetailContent);
            pnlRight.Controls.Add(lblNoSelection);
        }

        private Label[] CreateInfoBlock(Panel parent, string title, int startY, string[] fieldLabels)
        {
            var lblBlockTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeManager.Primary,
                Location = new Point(0, startY),
                AutoSize = true
            };
            parent.Controls.Add(lblBlockTitle);

            int y = startY + 30;
            var valueLabels = new List<Label>();

            foreach (var fieldLabel in fieldLabels)
            {
                var lbl = new Label
                {
                    Text = fieldLabel,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = ThemeManager.TextSecondary,
                    Location = new Point(20, y),
                    Size = new Size(150, 20)
                };

                var valueLabel = new Label
                {
                    Text = "—",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = ThemeManager.TextPrimary,
                    Location = new Point(180, y),
                    Size = new Size(400, 20)
                };

                parent.Controls.AddRange(new Control[] { lbl, valueLabel });
                valueLabels.Add(valueLabel);
                y += 25;
            }

            return valueLabels.ToArray();
        }

        private Button CreateActionButton(string text, int x, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(180, 40),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false,
                Visible = false
            };
            btn.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btn, 8);
            return btn;
        }

        private void SetupListColumns()
        {
            dgvRequests.Columns.Clear();
            UIHelper.AddColumn(dgvRequests, "MaYeuCau", "Mã YC", "MaYeuCau", 70);
            UIHelper.AddColumn(dgvRequests, "NgayGui", "Ngày tạo", "NgayGui", 90);
            UIHelper.AddColumn(dgvRequests, "MaPhong", "Phòng", "MaPhong", 90);
            UIHelper.AddColumn(dgvRequests, "GiaPhong", "Giá thuê", "GiaPhong", 110);
            UIHelper.AddColumn(dgvRequests, "TrangThai", "Trạng thái", "TrangThai", 120);
        }

        private async void LoadData()
        {
            try
            {
                var requests = (await _repo.GetByTenantAsync(_userId)).ToList();

                // Apply status filter
                var selectedStatus = cboTrangThai.SelectedItem?.ToString();
                if (selectedStatus != "Tất cả")
                {
                    if (selectedStatus == "Chờ duyệt")
                    {
                        var multiStatus = new[] { "Pending", "WaitingConfirm", "PendingApprove" };
                        requests = requests.Where(r => multiStatus.Contains(r.TrangThai)).ToList();
                    }
                    else
                    {
                        var statusFilter = selectedStatus switch
                        {
                            "Chờ thanh toán" => "PendingPayment",
                            "Đã duyệt" => "Approved",
                            "Từ chối" => "Rejected",
                            "Đã hủy" => "Canceled",
                            _ => (string?)null
                        };

                        if (statusFilter != null)
                        {
                            requests = requests.Where(r => r.TrangThai == statusFilter).ToList();
                        }
                    }
                }

                dgvRequests.DataSource = requests;

                // Format columns
                if (dgvRequests.Columns.Contains("NgayGui"))
                    dgvRequests.Columns["NgayGui"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
                if (dgvRequests.Columns.Contains("GiaPhong"))
                    dgvRequests.Columns["GiaPhong"]!.DefaultCellStyle.Format = "#,##0";

                // Stats
                var pending = requests.Count(r => r.TrangThai == "Pending" || r.TrangThai == "PendingPayment" || r.TrangThai == "WaitingConfirm" || r.TrangThai == "PendingApprove");
                var approved = requests.Count(r => r.TrangThai == "Approved");
                var rejected = requests.Count(r => r.TrangThai == "Rejected");
                lblStats.Text = $"Tổng: {requests.Count} | Chờ duyệt: {pending} | Đã duyệt: {approved} | Từ chối: {rejected}";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void DgvRequests_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvRequests.SelectedRows.Count == 0)
            {
                lblNoSelection.Visible = true;
                pnlDetailContent.Visible = false;
                _selectedRequest = null;
                return;
            }

            _selectedRequest = dgvRequests.SelectedRows[0].DataBoundItem as YeuCauThuePhong;
            if (_selectedRequest == null) return;

            LoadDetailPanel(_selectedRequest);
            lblNoSelection.Visible = false;
            pnlDetailContent.Visible = true;
        }

        private async void LoadDetailPanel(YeuCauThuePhong request)
        {
            // Header
            lblDetailTitle.Text = $"Chi tiết yêu cầu #{request.MaYeuCau}";
            lblDetailStatus.Text = GetStatusDisplay(request.TrangThai);
            lblDetailStatus.BackColor = GetStatusColor(request.TrangThai);
            lblDetailCreatedDate.Text = $"Ngày gửi: {request.NgayGui:dd/MM/yyyy}";

            // Room Info
            lblRoomBuilding.Text = request.TenToaNha ?? "—";
            lblRoomCode.Text = request.MaPhong ?? "—";
            lblRoomType.Text = request.DienTich.HasValue && request.SoNguoiToiDa.HasValue
                ? $"{request.DienTich:N0} m² - Tối đa {request.SoNguoiToiDa} người"
                : "—";
            lblRoomPrice.Text = request.GiaPhong.HasValue ? $"{request.GiaPhong:N0} VNĐ" : "—";
            lblStartDate.Text = request.NgayBatDauMongMuon.ToString("dd/MM/yyyy");
            lblNumPeople.Text = request.SoNguoi.ToString();
            lblDepositMonths.Text = "1 tháng"; // Default, could be calculated

            // Try to get payment info
            BookingRequestDTO? paymentInfo = null;
            try
            {
                var allRequests = await _paymentRepo.GetAllBookingRequestsAsync(null);
                paymentInfo = allRequests.FirstOrDefault(r => r.MaYeuCau == request.MaYeuCau);
            }
            catch { }

            // Payment Info
            if (paymentInfo?.MaThanhToan != null)
            {
                lblPaymentAmount.Text = paymentInfo.SoTienCoc.HasValue ? $"{paymentInfo.SoTienCoc:N0} VNĐ" : "—";
                lblPaymentMethod.Text = "QR Bank / Chuyển khoản";
                lblPaymentContent.Text = $"NTPRO_{request.MaYeuCau}_{request.MaPhong}";
                lblPaymentDate.Text = paymentInfo.NgayThanhToan.HasValue ? paymentInfo.NgayThanhToan.Value.ToString("dd/MM/yyyy HH:mm") : "—";
                lblPaymentStatus.Text = paymentInfo.TrangThaiThanhToanDisplay;
            }
            else
            {
                lblPaymentAmount.Text = request.GiaPhong.HasValue ? $"{request.GiaPhong:N0} VNĐ" : "—";
                lblPaymentMethod.Text = "Chưa có";
                lblPaymentContent.Text = "—";
                lblPaymentDate.Text = "—";
                lblPaymentStatus.Text = "Chưa thanh toán";
            }

            // Status Info
            lblRequestStatus.Text = GetStatusDisplay(request.TrangThai);
            lblProcessDate.Text = request.NgayXuLy.HasValue ? request.NgayXuLy.Value.ToString("dd/MM/yyyy") : "—";
            lblRejectReason.Text = !string.IsNullOrEmpty(request.LyDoTuChoi) ? request.LyDoTuChoi : "—";
            lblNote.Text = !string.IsNullOrEmpty(request.GhiChu) ? request.GhiChu : "—";

            UpdateButtonStates(request, paymentInfo);
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "PendingPayment" => "Chờ thanh toán",
                "WaitingConfirm" => "Chờ xác nhận TT",
                "PendingApprove" => "Chờ duyệt HĐ",
                "Pending" => "Chờ duyệt",
                "Approved" => "Đã duyệt",
                "Rejected" => "Từ chối",
                "Canceled" => "Đã hủy",
                _ => status
            };
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "PendingPayment" => ThemeManager.Warning,
                "WaitingConfirm" => Color.DarkOrange,
                "PendingApprove" => ThemeManager.Primary,
                "Pending" => ThemeManager.Warning,
                "Approved" => ThemeManager.Success,
                "Rejected" => ThemeManager.Error,
                "Canceled" => ThemeManager.Secondary,
                _ => ThemeManager.TextSecondary
            };
        }

        private void UpdateButtonStates(YeuCauThuePhong request, BookingRequestDTO? paymentInfo)
        {
            // Hide all buttons first
            btnPayDeposit.Enabled = false;
            btnPayDeposit.Visible = false;
            btnCancelRequest.Enabled = false;
            btnCancelRequest.Visible = false;
            btnViewContract.Enabled = false;
            btnViewContract.Visible = false;

            // Chờ thanh toán - hiển thị nút thanh toán
            if (request.TrangThai == "PendingPayment" || (paymentInfo?.TrangThaiThanhToan == "Pending"))
            {
                btnPayDeposit.Enabled = true;
                btnPayDeposit.Visible = true;
            }

            // Chờ duyệt hoặc chờ xác nhận - có thể hủy
            if (request.TrangThai == "Pending" || request.TrangThai == "WaitingConfirm" || request.TrangThai == "PendingApprove")
            {
                btnCancelRequest.Enabled = true;
                btnCancelRequest.Visible = true;
            }

            // Đã duyệt - hiển thị nút xem hợp đồng
            if (request.TrangThai == "Approved")
            {
                btnViewContract.Enabled = true;
                btnViewContract.Visible = true;
            }
        }

        private void DgvRequests_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvRequests.Columns[e.ColumnIndex].Name == "TrangThai" && e.Value != null)
            {
                var status = e.Value.ToString();
                e.Value = GetStatusDisplay(status ?? "");
                e.CellStyle.ForeColor = GetStatusColor(status ?? "");
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                e.FormattingApplied = true;
            }
        }

        private void BtnPayDeposit_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest == null) return;

            // TODO: Show QR payment dialog
            UIHelper.ShowWarning("Chức năng thanh toán QR đang được phát triển.\n\nVui lòng liên hệ quản lý để thanh toán trực tiếp.");
        }

        private async void BtnCancelRequest_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy yêu cầu thuê phòng #{_selectedRequest.MaYeuCau}?\n\n" +
                $"Phòng: {_selectedRequest.MaPhong}\n" +
                $"Ngày gửi: {_selectedRequest.NgayGui:dd/MM/yyyy}",
                "Xác nhận hủy yêu cầu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var (success, message) = await _repo.RejectAsync(
                    _selectedRequest.MaYeuCau,
                    _userId,
                    "Tenant tự hủy yêu cầu"
                );

                if (success)
                {
                    UIHelper.ShowSuccess("Đã hủy yêu cầu thành công!");
                    LoadData();
                }
                else
                {
                    UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi hủy yêu cầu: {ex.Message}");
            }
        }

        private void BtnViewContract_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest == null) return;

            // TODO: Navigate to contract view
            UIHelper.ShowWarning("Chức năng xem hợp đồng đang được phát triển.\n\nVui lòng liên hệ quản lý để xem chi tiết hợp đồng.");
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
