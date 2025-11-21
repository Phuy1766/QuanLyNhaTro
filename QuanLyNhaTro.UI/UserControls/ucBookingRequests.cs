using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    /// <summary>
    /// Trang duyệt yêu cầu thuê phòng (Admin/Manager)
    /// Redesigned với Master-Detail layout
    /// </summary>
    public partial class ucBookingRequests : UserControl
    {
        private readonly PaymentRepository _paymentRepo = new();
        private readonly YeuCauThuePhongRepository _repo = new();

        // Left Panel - List Controls
        private Panel pnlLeft = null!;
        private DataGridView dgvRequests = null!;
        private ComboBox cboTrangThai = null!;
        private ComboBox cboTimeRange = null!;
        private TextBox txtSearch = null!;
        private Label lblPendingCount = null!;

        // Right Panel - Detail Controls
        private Panel pnlRight = null!;
        private Panel pnlDetailContent = null!;
        private Label lblNoSelection = null!;

        // Detail Section Controls
        private Label lblDetailTitle = null!;
        private Label lblDetailStatus = null!;
        private Label lblDetailCreatedDate = null!;
        private Label lblDetailCreatedBy = null!;

        // Tenant Info Block
        private Label lblTenantName = null!;
        private Label lblTenantPhone = null!;
        private Label lblTenantEmail = null!;

        // Room Info Block
        private Label lblRoomBuilding = null!;
        private Label lblRoomCode = null!;
        private Label lblRoomType = null!;
        private Label lblRoomPrice = null!;
        private Label lblStartDate = null!;
        private Label lblDepositMonths = null!;

        // Payment Info Block
        private Label lblPaymentAmount = null!;
        private Label lblPaymentMethod = null!;
        private Label lblPaymentContent = null!;
        private Label lblPaymentDate = null!;
        private Label lblPaymentStatus = null!;

        // Action Buttons
        private Button btnConfirmPayment = null!;
        private Button btnCancelPayment = null!;
        private Button btnApprove = null!;
        private Button btnReject = null!;
        private Button btnCancel = null!;

        private BookingRequestDTO? _selectedRequest = null;

        public ucBookingRequests()
        {
            InitializeComponent();
            CreateLayout();
            // Responsive layout khi thay đổi kích thước
            this.Resize += (_, __) => ApplyResponsiveLayout();
            ApplyResponsiveLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            // Use #F5F7FB as specified in design
            this.BackColor = Color.FromArgb(245, 247, 251);
            this.Padding = new Padding(20);

            // ===== HEADER PANEL =====
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "Yêu cầu thuê phòng",
                Font = new Font("Segoe UI Semibold", 16),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Quản lý các yêu cầu thuê phòng và trạng thái thanh toán / duyệt hợp đồng.",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(0, 30),
                AutoSize = true
            };

            lblPendingCount = new Label
            {
                Text = "0 chờ xác nhận | 0 chờ duyệt HĐ",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.Warning,
                Location = new Point(0, 55),
                AutoSize = true
            };

            // Filter Row
            int filterY = 75;
            int filterX = 0;

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
                "Chờ xác nhận tiền",
                "Chờ duyệt",
                "Đã duyệt",
                "Từ chối",
                "Đã hủy"
            });
            cboTrangThai.SelectedIndex = 0;
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData();
            filterX += 200;

            var lblTime = new Label
            {
                Text = "Thời gian:",
                Location = new Point(filterX, filterY + 7),
                AutoSize = true,
                ForeColor = ThemeManager.TextPrimary
            };
            filterX += 80;

            cboTimeRange = new ComboBox
            {
                Location = new Point(filterX, filterY),
                Size = new Size(150, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cboTimeRange.Items.AddRange(new object[] { "7 ngày", "30 ngày", "Tất cả" });
            cboTimeRange.SelectedIndex = 2;
            cboTimeRange.SelectedIndexChanged += (s, e) => LoadData();
            filterX += 170;

            txtSearch = new TextBox
            {
                Location = new Point(filterX, filterY),
                Size = new Size(220, 30),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Tìm theo mã, tên khách, mã phòng..."
            };
            txtSearch.TextChanged += (s, e) => LoadData();
            filterX += 240;

            var btnRefresh = new Button
            {
                Text = "⟳",
                Location = new Point(filterX, filterY - 2),
                Size = new Size(36, 36),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnRefresh, 8);
            btnRefresh.Click += (s, e) => LoadData();

            pnlHeader.Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle, lblPendingCount,
                lblStatus, cboTrangThai, lblTime, cboTimeRange, txtSearch, btnRefresh
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
                Width = 480,
                BackColor = Color.White,
                Padding = new Padding(20, 20, 20, 20)
            };

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
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 42,
                GridColor = Color.FromArgb(229, 231, 235)
            };
            UIHelper.StyleDataGridView(dgvRequests);
            SetupListColumns();
            dgvRequests.SelectionChanged += DgvRequests_SelectionChanged;
            dgvRequests.CellFormatting += DgvRequests_CellFormatting;

            // Enhanced hover effect
            dgvRequests.CellMouseEnter += (s, e) => {
                if (e.RowIndex >= 0)
                    dgvRequests.Cursor = Cursors.Hand;
            };
            dgvRequests.CellMouseLeave += (s, e) => {
                dgvRequests.Cursor = Cursors.Default;
            };

            pnlLeft.Controls.Add(dgvRequests);
            pnlLeft.Controls.Add(lblListTitle);

            // ===== RIGHT PANEL - DETAIL (60%) =====
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 20, 20, 20)
            };

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

            // 40% cho panel trái, trừ đi khoảng cách giữa 2 panel (20px)
            int leftWidth = (int)((totalWidth - 20) * 0.42);

            // Giới hạn cho đẹp
            int minLeft = 420;
            int maxLeft = 650;
            if (leftWidth < minLeft) leftWidth = minLeft;
            if (leftWidth > maxLeft) leftWidth = maxLeft;

            pnlLeft.Width = leftWidth;
        }


        private void CreateDetailPanel()
        {
            // No Selection Message
            lblNoSelection = new Label
            {
                Text = "Chọn một yêu cầu để xem chi tiết",
                Font = new Font("Segoe UI", 12),
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
                Text = "Ngày tạo: 01/01/2025",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(0, 35),
                AutoSize = true
            };

            lblDetailCreatedBy = new Label
            {
                Text = "Người tạo: Nguyễn Văn A",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(200, 35),
                AutoSize = true
            };

            int y = 70;

            // ===== TENANT INFO BLOCK =====
            var tenantLabels = CreateInfoBlock(pnlDetailContent, "Thông tin khách thuê", y,
                new[] { "Họ tên:", "Số điện thoại:", "Email:" });
            lblTenantName = tenantLabels[0];
            lblTenantPhone = tenantLabels[1];
            lblTenantEmail = tenantLabels[2];
            y = tenantLabels[tenantLabels.Length - 1].Top + 25;

            y += 24;

            // ===== ROOM INFO BLOCK =====
            var roomLabels = CreateInfoBlock(pnlDetailContent, "Thông tin phòng đăng ký", y,
                new[] { "Tòa nhà:", "Mã phòng:", "Loại phòng:", "Giá thuê/tháng:", "Ngày dự kiến thuê:", "Số tháng cọc:" });
            lblRoomBuilding = roomLabels[0];
            lblRoomCode = roomLabels[1];
            lblRoomType = roomLabels[2];
            lblRoomPrice = roomLabels[3];
            lblStartDate = roomLabels[4];
            lblDepositMonths = roomLabels[5];
            y = roomLabels[roomLabels.Length - 1].Top + 25;

            y += 24;

            // ===== PAYMENT INFO BLOCK =====
            var paymentLabels = CreateInfoBlock(pnlDetailContent, "Thông tin thanh toán cọc", y,
                new[] { "Số tiền cọc:", "Hình thức:", "Nội dung CK:", "Thời gian TT:", "Trạng thái TT:" });
            lblPaymentAmount = paymentLabels[0];
            lblPaymentMethod = paymentLabels[1];
            lblPaymentContent = paymentLabels[2];
            lblPaymentDate = paymentLabels[3];
            lblPaymentStatus = paymentLabels[4];
            y = paymentLabels[paymentLabels.Length - 1].Top + 25;

            y += 32;

            // ===== ACTION BUTTONS =====
            var pnlActions = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(650, 50),
                BackColor = Color.Transparent
            };

            int btnX = 0;

            btnConfirmPayment = CreateActionButton("💵 Xác nhận đã nhận tiền", btnX, ThemeManager.Success);
            btnConfirmPayment.Click += BtnConfirmPayment_Click;
            btnX += 190;

            btnCancelPayment = CreateActionButton("✗ Hủy giao dịch", btnX, ThemeManager.Error);
            btnCancelPayment.Width = 140;
            btnCancelPayment.Click += BtnCancelPayment_Click;
            btnX += 150;

            btnApprove = CreateActionButton("✓ Duyệt & Tạo HĐ", btnX, ThemeManager.Primary);
            btnApprove.Click += BtnApprove_Click;
            btnX += 160;

            btnReject = CreateActionButton("✗ Từ chối", btnX, Color.FromArgb(239, 68, 68));
            btnReject.Width = 110;
            btnReject.Click += BtnReject_Click;
            btnX += 120;

            btnCancel = CreateActionButton("Hủy YC", btnX, ThemeManager.Secondary);
            btnCancel.Width = 90;
            btnCancel.Click += BtnCancel_Click;

            pnlActions.Controls.AddRange(new Control[] {
                btnConfirmPayment, btnCancelPayment, btnApprove, btnReject, btnCancel
            });

            pnlDetailContent.Controls.AddRange(new Control[] {
                lblDetailTitle, lblDetailStatus, lblDetailCreatedDate, lblDetailCreatedBy, pnlActions
            });

            pnlRight.Controls.Add(pnlDetailContent);
            pnlRight.Controls.Add(lblNoSelection);
        }

        private Label[] CreateInfoBlock(Panel parent, string title, int startY, string[] fieldLabels)
        {
            var lblBlockTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = ThemeManager.Primary,
                Location = new Point(0, startY),
                AutoSize = true
            };
            parent.Controls.Add(lblBlockTitle);

            int y = startY + 32;
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
                Enabled = false
            };
            btn.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btn, 8);
            return btn;
        }

        private void SetupListColumns()
        {
            dgvRequests.Columns.Clear();
            dgvRequests.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            UIHelper.AddColumn(dgvRequests, "MaYeuCau", "Mã YC", "MaYeuCau", 65);
            UIHelper.AddColumn(dgvRequests, "NgayGui", "Ngày", "NgayGui", 85);
            UIHelper.AddColumn(dgvRequests, "TenTenant", "Khách thuê", "TenTenant", 130);
            UIHelper.AddColumn(dgvRequests, "MaPhong", "Phòng", "MaPhong", 70);

            var colTrangThai = new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = "Trạng thái",
                DataPropertyName = "TrangThai",
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dgvRequests.Columns.Add(colTrangThai);
        }

        private string? GetFilterStatus()
        {
            var selected = cboTrangThai.SelectedItem?.ToString();
            return selected switch
            {
                "Chờ thanh toán" => "PendingPayment",
                "Chờ xác nhận tiền" => "WaitingConfirm",
                "Chờ duyệt" => "PendingApprove",
                "Đã duyệt" => "Approved",
                "Từ chối" => "Rejected",
                "Đã hủy" => "Canceled",
                _ => null
            };
        }

        private async void LoadData()
        {
            try
            {
                var trangThai = GetFilterStatus();
                var requests = await _paymentRepo.GetAllBookingRequestsAsync(trangThai);
                var dataList = requests.ToList();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    var searchTerm = txtSearch.Text.Trim().ToLower();
                    dataList = dataList.Where(r =>
                        r.MaYeuCau.ToString().Contains(searchTerm) ||
                        (r.TenTenant?.ToLower().Contains(searchTerm) ?? false) ||
                        (r.MaPhong?.ToLower().Contains(searchTerm) ?? false)
                    ).ToList();
                }

                // Apply time range filter
                if (cboTimeRange.SelectedItem?.ToString() != "Tất cả")
                {
                    var days = cboTimeRange.SelectedItem?.ToString() == "7 ngày" ? 7 : 30;
                    var cutoff = DateTime.Now.AddDays(-days);
                    dataList = dataList.Where(r => r.NgayGui >= cutoff).ToList();
                }

                dgvRequests.DataSource = dataList;

                // Format columns
                if (dgvRequests.Columns.Contains("NgayGui"))
                    dgvRequests.Columns["NgayGui"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
                if (dgvRequests.Columns.Contains("SoTienCoc"))
                    dgvRequests.Columns["SoTienCoc"]!.DefaultCellStyle.Format = "#,##0";

                // Update pending count
                var (waitingConfirm, pendingApprove) = await _paymentRepo.CountPendingRequestsAsync();
                lblPendingCount.Text = $"{waitingConfirm} chờ xác nhận | {pendingApprove} chờ duyệt HĐ";
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

            _selectedRequest = dgvRequests.SelectedRows[0].DataBoundItem as BookingRequestDTO;
            if (_selectedRequest == null) return;

            LoadDetailPanel(_selectedRequest);
            lblNoSelection.Visible = false;
            pnlDetailContent.Visible = true;
        }

        private void LoadDetailPanel(BookingRequestDTO request)
        {
            // Header
            lblDetailTitle.Text = $"Chi tiết yêu cầu #{request.MaYeuCau}";
            lblDetailStatus.Text = request.TrangThaiDisplay;
            lblDetailStatus.BackColor = GetStatusColor(request.TrangThai);
            lblDetailCreatedDate.Text = $"Ngày tạo: {request.NgayGui:dd/MM/yyyy}";
            lblDetailCreatedBy.Text = $"Người tạo: {request.TenTenant} (Tenant)";

            // Tenant Info
            lblTenantName.Text = request.TenTenant ?? "—";
            lblTenantPhone.Text = request.SdtTenant ?? "—";
            lblTenantEmail.Text = "—"; // Not in model

            // Room Info
            lblRoomBuilding.Text = request.TenToaNha ?? "—";
            lblRoomCode.Text = request.MaPhong ?? "—";
            lblRoomType.Text = $"{request.DienTich:N0} m² - {request.SoNguoiToiDa} người";
            lblRoomPrice.Text = $"{request.GiaPhong:N0} VNĐ";
            lblStartDate.Text = request.NgayBatDauMongMuon.ToString("dd/MM/yyyy");
            lblDepositMonths.Text = request.SoTienCoc.HasValue && request.GiaPhong.HasValue && request.GiaPhong > 0
                ? $"{(request.SoTienCoc.Value / request.GiaPhong.Value):N1} tháng"
                : "—";

            // Payment Info
            lblPaymentAmount.Text = request.SoTienCoc.HasValue ? $"{request.SoTienCoc:N0} VNĐ" : "—";
            lblPaymentMethod.Text = request.MaThanhToan.HasValue ? "QR Bank / Chuyển khoản" : "—";
            lblPaymentContent.Text = request.MaThanhToan.HasValue ? $"NTPRO_{request.MaYeuCau}_{request.MaPhong}" : "—";
            lblPaymentDate.Text = request.NgayThanhToan.HasValue ? request.NgayThanhToan.Value.ToString("dd/MM/yyyy HH:mm") : "—";
            lblPaymentStatus.Text = request.TrangThaiThanhToanDisplay;

            UpdateButtonStates(request);
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "PendingPayment" => ThemeManager.Warning,
                "WaitingConfirm" => Color.DarkOrange,
                "PendingApprove" => ThemeManager.Primary,
                "Approved" => ThemeManager.Success,
                "Rejected" => ThemeManager.Error,
                "Canceled" => ThemeManager.Secondary,
                _ => ThemeManager.TextSecondary
            };
        }

        private void UpdateButtonStates(BookingRequestDTO request)
        {
            btnConfirmPayment.Enabled = false;
            btnCancelPayment.Enabled = false;
            btnApprove.Enabled = false;
            btnReject.Enabled = false;
            btnCancel.Enabled = false;

            // Xác nhận thanh toán - chỉ khi trạng thái WaitingConfirm
            if (request.TrangThai == "WaitingConfirm" && request.TrangThaiThanhToan == "WaitingConfirm")
            {
                btnConfirmPayment.Enabled = true;
                btnCancelPayment.Enabled = true;
            }

            // Duyệt HĐ - chỉ khi trạng thái PendingApprove (đã thanh toán xong)
            if (request.TrangThai == "PendingApprove")
            {
                btnApprove.Enabled = true;
                btnReject.Enabled = true;
            }

            // Có thể từ chối nếu đang chờ thanh toán
            if (request.TrangThai == "PendingPayment" || request.TrangThai == "WaitingConfirm")
            {
                btnReject.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private void DgvRequests_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;

            var colName = dgvRequests.Columns[e.ColumnIndex].Name;

            if (colName == "TrangThai")
            {
                var status = e.Value.ToString();
                e.Value = status switch
                {
                    "PendingPayment" => "Chờ TT",
                    "WaitingConfirm" => "Chờ xác nhận",
                    "PendingApprove" => "Chờ duyệt",
                    "Approved" => "Đã duyệt",
                    "Rejected" => "Từ chối",
                    "Canceled" => "Đã hủy",
                    _ => status
                };
                e.CellStyle.ForeColor = GetStatusColor(status ?? "");
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                e.FormattingApplied = true;
            }
        }

        private async void BtnConfirmPayment_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest?.MaThanhToan == null) return;

            var result = MessageBox.Show(
                $"Xác nhận đã nhận tiền cọc từ khách thuê?\n\n" +
                $"Người thuê: {_selectedRequest.TenTenant}\n" +
                $"Phòng: {_selectedRequest.MaPhong}\n" +
                $"Số tiền: {_selectedRequest.SoTienCoc:N0} VND",
                "Xác nhận thanh toán",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var (success, message) = await _paymentRepo.AdminConfirmPaymentAsync(
                    _selectedRequest.MaThanhToan.Value,
                    AuthService.CurrentUser?.UserId ?? 0,
                    true,
                    "Admin xác nhận đã nhận tiền cọc"
                );

                if (success)
                {
                    UIHelper.ShowSuccess("Đã xác nhận thanh toán thành công!\nYêu cầu chuyển sang trạng thái chờ duyệt hợp đồng.");
                    LoadData();
                }
                else
                {
                    UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi xác nhận thanh toán: {ex.Message}");
            }
        }

        private async void BtnCancelPayment_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest?.MaThanhToan == null) return;

            var popup = new Form
            {
                Text = "Hủy giao dịch thanh toán",
                Size = new Size(400, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            var lblInfo = new Label
            {
                Text = $"Hủy thanh toán cho yêu cầu thuê phòng {_selectedRequest.MaPhong}\ncủa {_selectedRequest.TenTenant}",
                Location = new Point(20, 20),
                Size = new Size(340, 40),
                ForeColor = ThemeManager.TextPrimary
            };

            var lblReason = new Label
            {
                Text = "Lý do hủy:",
                Location = new Point(20, 70),
                AutoSize = true,
                ForeColor = ThemeManager.TextPrimary
            };

            var txtReason = new TextBox
            {
                Location = new Point(20, 95),
                Size = new Size(340, 60),
                Multiline = true,
                PlaceholderText = "Nhập lý do hủy giao dịch..."
            };

            var btnConfirm = new Button
            {
                Text = "Hủy giao dịch",
                Location = new Point(100, 170),
                Size = new Size(110, 35),
                BackColor = ThemeManager.Error,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            var btnCancelPopup = new Button
            {
                Text = "Đóng",
                Location = new Point(220, 170),
                Size = new Size(80, 35),
                BackColor = ThemeManager.Secondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelPopup.FlatAppearance.BorderSize = 0;
            btnCancelPopup.Click += (s, args) => popup.Close();

            btnConfirm.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    UIHelper.ShowWarning("Vui lòng nhập lý do hủy!");
                    return;
                }

                try
                {
                    var (success, message) = await _paymentRepo.AdminConfirmPaymentAsync(
                        _selectedRequest.MaThanhToan.Value,
                        AuthService.CurrentUser?.UserId ?? 0,
                        false,
                        txtReason.Text.Trim()
                    );

                    if (success)
                    {
                        UIHelper.ShowSuccess("Đã hủy giao dịch thanh toán!");
                        popup.Close();
                        LoadData();
                    }
                    else
                    {
                        UIHelper.ShowError(message);
                    }
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi hủy giao dịch: {ex.Message}");
                }
            };

            popup.Controls.AddRange(new Control[] { lblInfo, lblReason, txtReason, btnConfirm, btnCancelPopup });
            popup.ShowDialog();
        }

        private void BtnApprove_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest == null) return;
            ShowApprovePopup(_selectedRequest);
        }

        private void BtnReject_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest == null) return;
            ShowRejectPopup(_selectedRequest);
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest == null) return;
            ShowRejectPopup(_selectedRequest); // Can reuse the same popup
        }

        private void ShowApprovePopup(BookingRequestDTO request)
        {
            var popup = new Form
            {
                Text = "Duyệt yêu cầu - Tạo hợp đồng",
                Size = new Size(500, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            int y = 20;

            AddInfoRow(popup, "Người thuê:", request.TenTenant ?? "", ref y);
            AddInfoRow(popup, "SĐT:", request.SdtTenant ?? "", ref y);
            AddInfoRow(popup, "Phòng:", $"{request.MaPhong} - {request.TenToaNha}", ref y);
            AddInfoRow(popup, "Giá phòng:", $"{request.GiaPhong:N0} VND", ref y);
            AddInfoRow(popup, "Tiền cọc đã TT:", $"{request.SoTienCoc:N0} VND", ref y);
            AddInfoRow(popup, "Ngày BĐ mong muốn:", request.NgayBatDauMongMuon.ToString("dd/MM/yyyy"), ref y);
            AddInfoRow(popup, "Số người:", request.SoNguoi.ToString(), ref y);

            y += 15;

            var lblHD = new Label
            {
                Text = "═══ Thông tin hợp đồng ═══",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = ThemeManager.Primary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            popup.Controls.Add(lblHD);
            y += 30;

            var lblMaHD = new Label { Text = "Mã hợp đồng:", Location = new Point(20, y + 3), Width = 120, ForeColor = ThemeManager.TextPrimary };
            var txtMaHD = new TextBox
            {
                Location = new Point(150, y),
                Size = new Size(200, 30),
                Text = $"HD{DateTime.Now:yyyyMMddHHmm}"
            };
            popup.Controls.AddRange(new Control[] { lblMaHD, txtMaHD });
            y += 35;

            var lblNgayKT = new Label { Text = "Ngày kết thúc:", Location = new Point(20, y + 3), Width = 120, ForeColor = ThemeManager.TextPrimary };
            var dtpNgayKT = new DateTimePicker
            {
                Location = new Point(150, y),
                Size = new Size(200, 30),
                Format = DateTimePickerFormat.Short,
                Value = request.NgayBatDauMongMuon.AddYears(1)
            };
            popup.Controls.AddRange(new Control[] { lblNgayKT, dtpNgayKT });
            y += 35;

            var lblCoc = new Label { Text = "Tiền cọc:", Location = new Point(20, y + 3), Width = 120, ForeColor = ThemeManager.TextPrimary };
            var nudCoc = new NumericUpDown
            {
                Location = new Point(150, y),
                Size = new Size(150, 30),
                Maximum = 100000000,
                Value = request.SoTienCoc ?? request.GiaPhong ?? 0,
                ThousandsSeparator = true,
                Enabled = false
            };
            popup.Controls.AddRange(new Control[] { lblCoc, nudCoc });
            y += 35;

            var lblGhiChuHD = new Label { Text = "Ghi chú HĐ:", Location = new Point(20, y + 3), Width = 120, ForeColor = ThemeManager.TextPrimary };
            var txtGhiChuHD = new TextBox
            {
                Location = new Point(150, y),
                Size = new Size(300, 50),
                Multiline = true
            };
            popup.Controls.AddRange(new Control[] { lblGhiChuHD, txtGhiChuHD });
            y += 65;

            var btnApproveForm = new Button
            {
                Text = "✓ Duyệt & Tạo HĐ",
                Location = new Point(150, y),
                Size = new Size(140, 42),
                BackColor = ThemeManager.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApproveForm.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnApproveForm, 8);

            var btnCancelForm = new Button
            {
                Text = "Hủy",
                Location = new Point(300, y),
                Size = new Size(80, 42),
                BackColor = ThemeManager.Secondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelForm.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnCancelForm, 8);
            btnCancelForm.Click += (s, e) => popup.Close();

            btnApproveForm.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMaHD.Text))
                {
                    UIHelper.ShowWarning("Vui lòng nhập mã hợp đồng!");
                    return;
                }

                try
                {
                    var (success, message) = await _repo.ApproveAsync(
                        request.MaYeuCau,
                        AuthService.CurrentUser?.UserId ?? 0,
                        txtMaHD.Text.Trim(),
                        dtpNgayKT.Value.Date,
                        nudCoc.Value,
                        txtGhiChuHD.Text.Trim()
                    );

                    if (success)
                    {
                        UIHelper.ShowSuccess("Đã duyệt yêu cầu và tạo hợp đồng thành công!");
                        popup.Close();
                        LoadData();
                    }
                    else
                    {
                        UIHelper.ShowError(message);
                    }
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi duyệt yêu cầu: {ex.Message}");
                }
            };

            popup.Controls.AddRange(new Control[] { btnApproveForm, btnCancelForm });
            popup.ShowDialog();
        }

        private void ShowRejectPopup(BookingRequestDTO request)
        {
            var popup = new Form
            {
                Text = "Từ chối yêu cầu",
                Size = new Size(400, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            int y = 20;
            AddInfoRow(popup, "Người thuê:", request.TenTenant ?? "", ref y);
            AddInfoRow(popup, "Phòng:", request.MaPhong ?? "", ref y);

            y += 10;

            var lblLyDo = new Label
            {
                Text = "Lý do từ chối:",
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = ThemeManager.TextPrimary
            };
            popup.Controls.Add(lblLyDo);
            y += 25;

            var txtLyDo = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(340, 80),
                Multiline = true
            };
            popup.Controls.Add(txtLyDo);
            y += 95;

            var btnRejectForm = new Button
            {
                Text = "✗ Từ chối",
                Location = new Point(100, y),
                Size = new Size(100, 40),
                BackColor = ThemeManager.Error,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRejectForm.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnRejectForm, 8);

            var btnCancelForm = new Button
            {
                Text = "Hủy",
                Location = new Point(210, y),
                Size = new Size(80, 40),
                BackColor = ThemeManager.Secondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelForm.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnCancelForm, 8);
            btnCancelForm.Click += (s, e) => popup.Close();

            btnRejectForm.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtLyDo.Text))
                {
                    UIHelper.ShowWarning("Vui lòng nhập lý do từ chối!");
                    return;
                }

                try
                {
                    var (success, message) = await _repo.RejectAsync(
                        request.MaYeuCau,
                        AuthService.CurrentUser?.UserId ?? 0,
                        txtLyDo.Text.Trim()
                    );

                    if (success)
                    {
                        UIHelper.ShowSuccess("Đã từ chối yêu cầu!");
                        popup.Close();
                        LoadData();
                    }
                    else
                    {
                        UIHelper.ShowError(message);
                    }
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi từ chối yêu cầu: {ex.Message}");
                }
            };

            popup.Controls.AddRange(new Control[] { btnRejectForm, btnCancelForm });
            popup.ShowDialog();
        }

        private void AddInfoRow(Form form, string label, string value, ref int y)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(20, y),
                Width = 130,
                ForeColor = ThemeManager.TextSecondary
            };
            var lblValue = new Label
            {
                Text = value,
                Location = new Point(150, y),
                Width = 300,
                ForeColor = ThemeManager.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            form.Controls.AddRange(new Control[] { lbl, lblValue });
            y += 25;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucBookingRequests";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }
}
