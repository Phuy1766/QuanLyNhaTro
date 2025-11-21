using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    /// <summary>
    /// Trang tìm phòng trống và đăng ký thuê (Tenant) - Redesigned
    /// </summary>
    public partial class ucAvailableRooms : UserControl
    {
        private readonly YeuCauThuePhongRepository _repo = new();
        private readonly PaymentRepository _paymentRepo = new();
        private readonly BuildingRepository _buildingRepo = new();
        private readonly int _userId;

        // Controls
        private FlowLayoutPanel flpRooms = null!;
        private ComboBox cboToaNha = null!;
        private ComboBox cboTang = null!;
        private NumericUpDown nudGiaMin = null!;
        private NumericUpDown nudGiaMax = null!;
        private ComboBox cboSoNguoi = null!;
        private Button btnFilter = null!;
        private Button btnReset = null!;
        private Label lblNoData = null!;
        private Label lblCount = null!;

        public ucAvailableRooms(int userId)
        {
            _userId = userId;
            InitializeComponent();
            CreateLayout();
            LoadFilters();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = Color.FromArgb(243, 244, 246); // #F3F4F6
            this.Padding = new Padding(20);

            // ===== MAIN CONTAINER =====
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // ===== FILTER CARD =====
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.White,
                Padding = new Padding(25, 20, 25, 20)
            };
            UIHelper.RoundControl(pnlFilter, 16);
            AddShadow(pnlFilter);

            // Title
            var lblTitle = new Label
            {
                Text = "🔍 Tìm phòng trống",
                Font = new Font("Segoe UI Semibold", 16),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(25, 15),
                AutoSize = true
            };
            pnlFilter.Controls.Add(lblTitle);

            // Filter controls - Row 1
            int y1 = 50;
            int x = 25;

            // Tòa nhà
            var lblToaNha = new Label { Text = "Tòa nhà:", Location = new Point(x, y1 + 3), AutoSize = true, ForeColor = ThemeManager.TextSecondary };
            cboToaNha = CreateComboBox(x + 60, y1, 150);
            pnlFilter.Controls.AddRange(new Control[] { lblToaNha, cboToaNha });
            x += 230;

            // Tầng
            var lblTang = new Label { Text = "Tầng:", Location = new Point(x, y1 + 3), AutoSize = true, ForeColor = ThemeManager.TextSecondary };
            cboTang = CreateComboBox(x + 45, y1, 80);
            cboTang.Items.AddRange(new object[] { "Tất cả", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            cboTang.SelectedIndex = 0;
            pnlFilter.Controls.AddRange(new Control[] { lblTang, cboTang });
            x += 150;

            // Giá từ
            var lblGia = new Label { Text = "Giá (triệu):", Location = new Point(x, y1 + 3), AutoSize = true, ForeColor = ThemeManager.TextSecondary };
            nudGiaMin = CreateNumericUpDown(x + 70, y1, 80, 0, 100, 0);
            var lblDen = new Label { Text = "-", Location = new Point(x + 155, y1 + 3), AutoSize = true, ForeColor = ThemeManager.TextSecondary };
            nudGiaMax = CreateNumericUpDown(x + 170, y1, 80, 0, 100, 50);
            pnlFilter.Controls.AddRange(new Control[] { lblGia, nudGiaMin, lblDen, nudGiaMax });
            x += 270;

            // Số người
            var lblSoNguoi = new Label { Text = "Số người:", Location = new Point(x, y1 + 3), AutoSize = true, ForeColor = ThemeManager.TextSecondary };
            cboSoNguoi = CreateComboBox(x + 65, y1, 80);
            cboSoNguoi.Items.AddRange(new object[] { "Tất cả", "1", "2", "3", "4", "5+" });
            cboSoNguoi.SelectedIndex = 0;
            pnlFilter.Controls.AddRange(new Control[] { lblSoNguoi, cboSoNguoi });
            x += 170;

            // Buttons
            btnFilter = CreateButton("🔍 Lọc kết quả", x, y1 - 2, 120, ThemeManager.Primary);
            btnFilter.Click += BtnFilter_Click;
            btnReset = CreateButton("↻ Xóa lọc", x + 130, y1 - 2, 100, ThemeManager.Secondary);
            btnReset.Click += BtnReset_Click;
            pnlFilter.Controls.AddRange(new Control[] { btnFilter, btnReset });

            // Result count
            lblCount = new Label
            {
                Text = "0 phòng",
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeManager.TextMuted,
                Location = new Point(25, 95),
                AutoSize = true
            };
            pnlFilter.Controls.Add(lblCount);

            // ===== ROOMS LIST PANEL =====
            var pnlRoomList = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 15, 0, 0)
            };

            // Scrollable container for room cards
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            flpRooms = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            scrollPanel.Controls.Add(flpRooms);

            // No data label
            lblNoData = new Label
            {
                Text = "📭 Hiện chưa có phòng trống phù hợp với tiêu chí của bạn.\nHãy thử điều chỉnh bộ lọc để tìm kiếm.",
                Font = new Font("Segoe UI", 12),
                ForeColor = ThemeManager.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            scrollPanel.Controls.Add(lblNoData);

            pnlRoomList.Controls.Add(scrollPanel);

            // Add to main panel
            mainPanel.Controls.Add(pnlRoomList);
            mainPanel.Controls.Add(pnlFilter);

            this.Controls.Add(mainPanel);
        }

        private ComboBox CreateComboBox(int x, int y, int width)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
        }

        private NumericUpDown CreateNumericUpDown(int x, int y, int width, decimal min, decimal max, decimal value)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Minimum = min,
                Maximum = max,
                Value = value,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Font = new Font("Segoe UI", 9)
            };
        }

        private Button CreateButton(string text, int x, int y, int width, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 35),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btn, 8);
            return btn;
        }

        private void AddShadow(Control control)
        {
            // Simple shadow effect using margin
            control.Margin = new Padding(0, 0, 0, 5);
        }

        private async void LoadFilters()
        {
            try
            {
                var buildingList = (await _buildingRepo.GetAllAsync()).ToList();
                buildingList.Insert(0, new Building { BuildingCode = "", BuildingName = "-- Tất cả tòa nhà --" });
                cboToaNha.DataSource = buildingList;
                cboToaNha.DisplayMember = "BuildingName";
                cboToaNha.ValueMember = "BuildingCode";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private async void LoadData()
        {
            try
            {
                string? buildingCode = cboToaNha.SelectedValue?.ToString();
                if (string.IsNullOrEmpty(buildingCode)) buildingCode = null;

                decimal? giaMin = nudGiaMin.Value > 0 ? nudGiaMin.Value * 1000000 : null;
                decimal? giaMax = nudGiaMax.Value < 50 ? nudGiaMax.Value * 1000000 : null;

                int? soNguoi = null;
                if (cboSoNguoi.SelectedIndex > 0)
                {
                    var soNguoiText = cboSoNguoi.SelectedItem?.ToString()?.Replace("+", "");
                    if (int.TryParse(soNguoiText, out int sn)) soNguoi = sn;
                }

                var rooms = (await _repo.GetAvailableRoomsAsync(_userId, buildingCode, giaMin, giaMax, soNguoi)).ToList();

                // Filter by floor if selected
                if (cboTang.SelectedIndex > 0)
                {
                    var tang = int.Parse(cboTang.SelectedItem?.ToString() ?? "0");
                    rooms = rooms.Where(r => r.Tang == tang).ToList();
                }

                // Update UI
                lblCount.Text = $"{rooms.Count} phòng trống";
                lblNoData.Visible = rooms.Count == 0;
                flpRooms.Visible = rooms.Count > 0;

                // Clear and rebuild room cards
                flpRooms.Controls.Clear();
                foreach (var room in rooms)
                {
                    var card = CreateRoomCard(room);
                    flpRooms.Controls.Add(card);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải danh sách phòng: {ex.Message}");
            }
        }

        private Panel CreateRoomCard(PhongTrongDTO room)
        {
            var card = new Panel
            {
                Size = new Size(320, 220),
                BackColor = Color.White,
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };
            UIHelper.RoundControl(card, 12);

            int y = 15;
            int padding = 15;

            // Header: Room code + Building
            var lblHeader = new Label
            {
                Text = $"🏠 {room.MaPhong}",
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = ThemeManager.Primary,
                Location = new Point(padding, y),
                AutoSize = true
            };
            card.Controls.Add(lblHeader);

            var lblBuilding = new Label
            {
                Text = room.TenToaNha ?? "",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextMuted,
                Location = new Point(padding, y + 25),
                AutoSize = true
            };
            card.Controls.Add(lblBuilding);
            y += 55;

            // Info rows
            AddCardInfo(card, $"📐 Diện tích: {room.DienTich} m²", padding, ref y);
            AddCardInfo(card, $"🏢 Tầng: {room.Tang}", padding, ref y);
            AddCardInfo(card, $"👥 Tối đa: {room.SoNguoiToiDa} người", padding, ref y);
            AddCardInfo(card, $"🏷️ Loại: {room.LoaiPhong ?? "Phòng thường"}", padding, ref y);

            // Price
            var lblPrice = new Label
            {
                Text = UIHelper.FormatCurrency(room.GiaThue) + "/tháng",
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = ThemeManager.Success,
                Location = new Point(padding, y + 5),
                AutoSize = true
            };
            card.Controls.Add(lblPrice);

            // Button
            var btnText = room.HasPendingRequest ? "📋 Đã gửi yêu cầu" : "✨ Thuê & Thanh toán online";
            var btnColor = room.HasPendingRequest ? ThemeManager.Warning : ThemeManager.Primary;

            var btnBook = new Button
            {
                Text = btnText,
                Size = new Size(140, 35),
                Location = new Point(card.Width - 155, y),
                BackColor = btnColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = room.HasPendingRequest ? Cursors.Default : Cursors.Hand,
                Enabled = !room.HasPendingRequest
            };
            btnBook.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnBook, 8);
            btnBook.Click += (s, e) => ShowBookingPaymentForm(room);
            card.Controls.Add(btnBook);

            return card;
        }

        private void AddCardInfo(Panel card, string text, int x, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(x, y),
                AutoSize = true
            };
            card.Controls.Add(lbl);
            y += 22;
        }

        private void BtnFilter_Click(object? sender, EventArgs e) => LoadData();

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            cboToaNha.SelectedIndex = 0;
            cboTang.SelectedIndex = 0;
            nudGiaMin.Value = 0;
            nudGiaMax.Value = 50;
            cboSoNguoi.SelectedIndex = 0;
            LoadData();
        }

        private async void ShowBookingPaymentForm(PhongTrongDTO room)
        {
            // Get default payment config
            var paymentConfig = await _paymentRepo.GetDefaultConfigAsync();
            if (paymentConfig == null)
            {
                UIHelper.ShowError("Chưa có cấu hình thanh toán. Vui lòng liên hệ quản trị viên.");
                return;
            }

            var depositAmount = room.GiaThue * paymentConfig.DepositMonths;

            var popup = new Form
            {
                Text = "Đăng ký thuê phòng & Thanh toán cọc online",
                Size = new Size(550, 680),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            int y = 20;
            int lblWidth = 130;

            // === THÔNG TIN PHÒNG ===
            var lblSection1 = CreateSectionLabel("📋 Thông tin phòng", y);
            scrollPanel.Controls.Add(lblSection1);
            y += 35;

            AddInfoRow(scrollPanel, "Mã phòng:", room.MaPhong, ref y);
            AddInfoRow(scrollPanel, "Tòa nhà:", room.TenToaNha ?? "", ref y);
            AddInfoRow(scrollPanel, "Tầng:", room.Tang?.ToString() ?? "", ref y);
            AddInfoRow(scrollPanel, "Diện tích:", $"{room.DienTich} m²", ref y);
            AddInfoRow(scrollPanel, "Giá thuê:", UIHelper.FormatCurrency(room.GiaThue) + "/tháng", ref y);
            AddInfoRow(scrollPanel, "Số người tối đa:", room.SoNguoiToiDa.ToString(), ref y);

            y += 10;

            // === THÔNG TIN ĐĂNG KÝ ===
            var lblSection2 = CreateSectionLabel("📝 Thông tin đăng ký", y);
            scrollPanel.Controls.Add(lblSection2);
            y += 35;

            // Ngày bắt đầu
            var lblNgay = new Label { Text = "Ngày bắt đầu:", Location = new Point(20, y + 3), Width = lblWidth, ForeColor = ThemeManager.TextPrimary };
            var dtpNgayBatDau = new DateTimePicker
            {
                Location = new Point(160, y),
                Size = new Size(200, 30),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today
            };
            scrollPanel.Controls.AddRange(new Control[] { lblNgay, dtpNgayBatDau });
            y += 35;

            // Số người ở
            var lblSoNguoi = new Label { Text = "Số người ở:", Location = new Point(20, y + 3), Width = lblWidth, ForeColor = ThemeManager.TextPrimary };
            var nudSoNguoiO = new NumericUpDown
            {
                Location = new Point(160, y),
                Size = new Size(80, 30),
                Minimum = 1,
                Maximum = room.SoNguoiToiDa,
                Value = 1
            };
            scrollPanel.Controls.AddRange(new Control[] { lblSoNguoi, nudSoNguoiO });
            y += 35;

            // Ghi chú
            var lblGhiChu = new Label { Text = "Ghi chú:", Location = new Point(20, y + 3), Width = lblWidth, ForeColor = ThemeManager.TextPrimary };
            var txtGhiChu = new TextBox
            {
                Location = new Point(160, y),
                Size = new Size(340, 50),
                Multiline = true
            };
            scrollPanel.Controls.AddRange(new Control[] { lblGhiChu, txtGhiChu });
            y += 65;

            // === THÔNG TIN THANH TOÁN CỌC ===
            var lblSection3 = CreateSectionLabel("💳 Thông tin thanh toán cọc", y);
            scrollPanel.Controls.Add(lblSection3);
            y += 35;

            AddInfoRow(scrollPanel, "Tiền cọc:", UIHelper.FormatCurrency(depositAmount), ref y, ThemeManager.Error);
            AddInfoRow(scrollPanel, "Ngân hàng:", paymentConfig.BankName, ref y);
            AddInfoRow(scrollPanel, "Chủ TK:", paymentConfig.AccountName, ref y);
            AddInfoRow(scrollPanel, "Số TK:", paymentConfig.AccountNumber, ref y);

            y += 20;

            // Buttons
            var btnCreate = new Button
            {
                Text = "✨ Tạo yêu cầu & Xem QR thanh toán",
                Location = new Point(160, y),
                Size = new Size(220, 45),
                BackColor = ThemeManager.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10),
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnCreate, 10);

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(390, y),
                Size = new Size(80, 45),
                BackColor = ThemeManager.Secondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnCancel, 10);
            btnCancel.Click += (s, e) => popup.Close();

            btnCreate.Click += async (s, e) =>
            {
                try
                {
                    btnCreate.Enabled = false;
                    btnCreate.Text = "Đang xử lý...";

                    var result = await _paymentRepo.CreateBookingWithPaymentAsync(
                        room.PhongId,
                        _userId,
                        dtpNgayBatDau.Value.Date,
                        (int)nudSoNguoiO.Value,
                        txtGhiChu.Text.Trim(),
                        paymentConfig.ConfigId
                    );

                    if (result.Success)
                    {
                        popup.Close();
                        ShowPaymentQRForm(result.MaYeuCau, result.MaThanhToan, room, paymentConfig, depositAmount);
                        LoadData();
                    }
                    else
                    {
                        UIHelper.ShowError(result.Message);
                        btnCreate.Enabled = true;
                        btnCreate.Text = "✨ Tạo yêu cầu & Xem QR thanh toán";
                    }
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi: {ex.Message}");
                    btnCreate.Enabled = true;
                    btnCreate.Text = "✨ Tạo yêu cầu & Xem QR thanh toán";
                }
            };

            scrollPanel.Controls.AddRange(new Control[] { btnCreate, btnCancel });
            popup.Controls.Add(scrollPanel);
            popup.ShowDialog();
        }

        private void ShowPaymentQRForm(int maYeuCau, int maThanhToan, PhongTrongDTO room, PaymentConfig config, decimal amount)
        {
            var transferContent = config.TransferTemplate
                .Replace("{MaYeuCau}", maYeuCau.ToString())
                .Replace("{MaPhong}", room.MaPhong);

            var popup = new Form
            {
                Text = "Thanh toán cọc thuê phòng online",
                Size = new Size(500, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(20)
            };

            int y = 10;

            // Title
            var lblTitle = new Label
            {
                Text = "💳 Quét mã QR để thanh toán",
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = ThemeManager.Primary,
                Location = new Point(20, y),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblTitle);
            y += 40;

            // QR Code - Using VietQR API
            var picQR = new PictureBox
            {
                Size = new Size(260, 260),
                Location = new Point((440 - 260) / 2, y),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            // Loading indicator
            var lblLoading = new Label
            {
                Text = "⏳ Đang tải QR...",
                Location = new Point(picQR.Left + 70, picQR.Top + 110),
                AutoSize = true,
                ForeColor = ThemeManager.TextSecondary
            };
            scrollPanel.Controls.AddRange(new Control[] { picQR, lblLoading });

            // Load QR asynchronously from VietQR API
            _ = Task.Run(async () =>
            {
                try
                {
                    var bankCode = config.BankCode ?? "VCB";
                    var qrBitmap = await QRCodeHelper.GetVietQRImageByBankCodeAsync(
                        bankCode,
                        config.AccountNumber,
                        amount,
                        transferContent,
                        config.AccountName,
                        "compact2"
                    );

                    // Update UI on main thread
                    if (picQR.IsHandleCreated)
                    {
                        picQR.Invoke(() =>
                        {
                            if (qrBitmap != null)
                            {
                                picQR.Image = qrBitmap;
                            }
                            else
                            {
                                // Fallback to offline QR
                                var fallbackQR = QRCodeHelper.GenerateQRCode(
                                    $"Bank: {bankCode}\nSTK: {config.AccountNumber}\nSố tiền: {amount:N0}\nND: {transferContent}",
                                    6
                                );
                                picQR.Image = fallbackQR;
                            }

                            if (lblLoading.Parent != null)
                            {
                                scrollPanel.Controls.Remove(lblLoading);
                                lblLoading.Dispose();
                            }
                        });
                    }
                }
                catch
                {
                    // Fallback on error
                    if (picQR.IsHandleCreated)
                    {
                        picQR.Invoke(() =>
                        {
                            var fallbackQR = QRCodeHelper.GenerateQRCode(
                                $"STK: {config.AccountNumber}\nSố tiền: {amount:N0}\nND: {transferContent}",
                                6
                            );
                            picQR.Image = fallbackQR;

                            if (lblLoading.Parent != null)
                            {
                                scrollPanel.Controls.Remove(lblLoading);
                                lblLoading.Dispose();
                            }
                        });
                    }
                }
            });

            y += 275;

            // Payment info
            var lblInfo = new Label
            {
                Text = "Thông tin chuyển khoản:",
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(20, y),
                AutoSize = true
            };
            scrollPanel.Controls.Add(lblInfo);
            y += 30;

            AddInfoRow(scrollPanel, "Ngân hàng:", config.BankName, ref y);
            AddInfoRow(scrollPanel, "Chủ TK:", config.AccountName, ref y);
            AddInfoRow(scrollPanel, "Số TK:", config.AccountNumber, ref y);
            AddInfoRow(scrollPanel, "Số tiền:", UIHelper.FormatCurrency(amount), ref y, ThemeManager.Error);

            // Transfer content with copy button
            var lblND = new Label { Text = "Nội dung CK:", Location = new Point(20, y), Width = 90, ForeColor = ThemeManager.TextSecondary };
            var txtND = new TextBox
            {
                Text = transferContent,
                Location = new Point(115, y - 2),
                Size = new Size(220, 25),
                ReadOnly = true,
                Font = new Font("Segoe UI Semibold", 9),
                BackColor = Color.FromArgb(255, 255, 230)
            };
            var btnCopy = new Button
            {
                Text = "📋 Copy",
                Location = new Point(345, y - 3),
                Size = new Size(70, 28),
                BackColor = ThemeManager.Info,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(transferContent);
                UIHelper.ShowSuccess("Đã copy nội dung chuyển khoản!");
            };
            scrollPanel.Controls.AddRange(new Control[] { lblND, txtND, btnCopy });
            y += 45;

            // Warning
            var lblWarning = new Label
            {
                Text = "⚠️ QUAN TRỌNG: Vui lòng chuyển khoản đúng số tiền và nội dung để được xử lý nhanh chóng!",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.Warning,
                Location = new Point(20, y),
                Size = new Size(420, 40)
            };
            scrollPanel.Controls.Add(lblWarning);
            y += 50;

            // Buttons
            var btnConfirm = new Button
            {
                Text = "✅ Tôi đã thanh toán",
                Location = new Point(100, y),
                Size = new Size(150, 45),
                BackColor = ThemeManager.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnConfirm, 10);

            var btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(260, y),
                Size = new Size(100, 45),
                BackColor = ThemeManager.Secondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnClose, 10);
            btnClose.Click += (s, e) => popup.Close();

            btnConfirm.Click += async (s, e) =>
            {
                if (MessageBox.Show(
                    "Bạn xác nhận đã chuyển khoản thành công?\n\nSau khi xác nhận, quản lý sẽ kiểm tra và phê duyệt yêu cầu của bạn.",
                    "Xác nhận thanh toán",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        var (success, message) = await _paymentRepo.ConfirmPaymentByTenantAsync(maThanhToan, _userId);
                        if (success)
                        {
                            UIHelper.ShowSuccess(message);
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
                        UIHelper.ShowError($"Lỗi: {ex.Message}");
                    }
                }
            };

            scrollPanel.Controls.AddRange(new Control[] { btnConfirm, btnClose });
            popup.Controls.Add(scrollPanel);
            popup.ShowDialog();
        }

        private Label CreateSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeManager.Primary,
                Location = new Point(20, y),
                AutoSize = true
            };
        }

        private void AddInfoRow(Control parent, string label, string value, ref int y, Color? valueColor = null)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(20, y),
                Width = 130,
                ForeColor = ThemeManager.TextSecondary,
                Font = new Font("Segoe UI", 9)
            };
            var lblValue = new Label
            {
                Text = value,
                Location = new Point(160, y),
                Width = 300,
                ForeColor = valueColor ?? ThemeManager.TextPrimary,
                Font = new Font("Segoe UI Semibold", 9)
            };
            parent.Controls.AddRange(new Control[] { lbl, lblValue });
            y += 28;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucAvailableRooms";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }
}
