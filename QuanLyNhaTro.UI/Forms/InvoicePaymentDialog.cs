using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.Forms
{
    /// <summary>
    /// Dialog thanh toán hóa đơn với QR code
    /// </summary>
    public partial class InvoicePaymentDialog : Form
    {
        private readonly HoaDon _hoaDon;
        private readonly PaymentRepository _paymentRepo = new();
        private readonly int _tenantUserId;
        private PictureBox _pbQRCode = null!;
        private Label _lblBankInfo = null!;
        private Label _lblAmount = null!;
        private Label _lblTransferContent = null!;
        private Button _btnConfirmPayment = null!;
        private Button _btnCancel = null!;
        private PaymentConfig? _paymentConfig;

        public bool PaymentConfirmed { get; private set; } = false;

        public InvoicePaymentDialog(HoaDon hoaDon, int tenantUserId)
        {
            _hoaDon = hoaDon;
            _tenantUserId = tenantUserId;
            InitializeComponent();
            BuildUI();
            LoadPaymentInfoAsync();
        }

        private void BuildUI()
        {
            // Form settings
            this.Text = "Thanh toán hóa đơn";
            this.Size = new Size(650, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.Padding = new Padding(15);

            // Main panel
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.White,
                AutoScroll = true
            };
            UIHelper.ApplyCardShadow(pnlMain);

            // Header
            var lblHeader = new Label
            {
                Text = "💳 Thanh toán hóa đơn",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                AutoSize = true,
                Location = new Point(25, 15)
            };

            // Invoice info panel
            var pnlInvoiceInfo = new Panel
            {
                Location = new Point(25, 60),
                Size = new Size(575, 120),
                BackColor = Color.FromArgb(240, 248, 255),
                Padding = new Padding(15)
            };

            var lblInvoiceInfo = new Label
            {
                Text = $"📄 Hóa đơn: {_hoaDon.MaHoaDon}\n" +
                       $"📅 Tháng: {_hoaDon.ThangNam:MM/yyyy}\n" +
                       $"💵 Tổng tiền: {_hoaDon.TongCong:N0} VNĐ\n" +
                       $"💰 Còn nợ: {_hoaDon.ConNo:N0} VNĐ",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = true,
                Location = new Point(15, 15)
            };
            pnlInvoiceInfo.Controls.Add(lblInvoiceInfo);

            // Separator
            var separator1 = new Panel
            {
                Location = new Point(25, 195),
                Size = new Size(575, 2),
                BackColor = Color.FromArgb(220, 220, 220)
            };

            // QR Code section
            var lblQRTitle = new Label
            {
                Text = "🔲 Quét mã QR để thanh toán",
                Font = new Font("Segoe UI Semibold", 13F),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = true,
                Location = new Point(25, 210)
            };

            _pbQRCode = new PictureBox
            {
                Location = new Point(175, 250),
                Size = new Size(300, 300),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };

            // Separator
            var separator2 = new Panel
            {
                Location = new Point(25, 565),
                Size = new Size(575, 2),
                BackColor = Color.FromArgb(220, 220, 220)
            };

            // Bank info panel
            var pnlBankInfo = new Panel
            {
                Location = new Point(25, 580),
                Size = new Size(575, 90),
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15)
            };

            _lblBankInfo = new Label
            {
                Text = "Đang tải thông tin ngân hàng...",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = true,
                Location = new Point(15, 15),
                MaximumSize = new Size(545, 0)
            };
            pnlBankInfo.Controls.Add(_lblBankInfo);

            // Amount
            _lblAmount = new Label
            {
                Text = $"💳 Số tiền cần thanh toán: {_hoaDon.ConNo:N0} VNĐ",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69),
                AutoSize = true,
                Location = new Point(25, 685)
            };

            // Transfer content
            _lblTransferContent = new Label
            {
                Text = "📝 Nội dung CK: ",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = true,
                Location = new Point(25, 715)
            };

            // Buttons
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(25, 15, 25, 15)
            };

            // Separator trên buttons
            var separatorTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };
            pnlButtons.Controls.Add(separatorTop);

            _btnConfirmPayment = new Button
            {
                Text = "✓ Đã thanh toán",
                Size = new Size(220, 45),
                Location = new Point(160, 17),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnConfirmPayment.FlatAppearance.BorderSize = 0;
            _btnConfirmPayment.Click += BtnConfirmPayment_Click;

            _btnCancel = new Button
            {
                Text = "✕ Đóng",
                Size = new Size(120, 45),
                Location = new Point(395, 17),
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => this.Close();

            pnlButtons.Controls.AddRange(new Control[] { _btnConfirmPayment, _btnCancel });

            // Add all controls
            pnlMain.Controls.AddRange(new Control[]
            {
                lblHeader, pnlInvoiceInfo, separator1, lblQRTitle, _pbQRCode,
                separator2, pnlBankInfo, _lblAmount, _lblTransferContent
            });

            this.Controls.AddRange(new Control[] { pnlMain, pnlButtons });
        }

        private async void LoadPaymentInfoAsync()
        {
            try
            {
                // Lấy cấu hình thanh toán
                _paymentConfig = await _paymentRepo.GetDefaultConfigAsync();

                if (_paymentConfig == null)
                {
                    MessageBox.Show("Chưa có cấu hình thanh toán. Vui lòng liên hệ quản lý.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                    return;
                }

                // Tạo nội dung chuyển khoản
                var transferContent = $"HD {_hoaDon.MaHoaDon} {_hoaDon.ThangNam:MMyyyy}";

                // Cập nhật thông tin
                _lblBankInfo.Text = $"🏦 Ngân hàng: {_paymentConfig.BankName}\n" +
                                   $"💳 Số tài khoản: {_paymentConfig.AccountNumber}\n" +
                                   $"👤 Chủ tài khoản: {_paymentConfig.AccountName}";

                _lblTransferContent.Text = $"📝 Nội dung CK: {transferContent}";

                // Tạo QR code với loading indicator
                _pbQRCode.Image = null;

                // Tạo loading panel
                var loadingPanel = new Panel
                {
                    Location = new Point(175, 250),
                    Size = new Size(300, 300),
                    BackColor = Color.FromArgb(248, 249, 250),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var loadingLabel = new Label
                {
                    Text = "⏳ Đang tải QR Code...\n\nVui lòng chờ",
                    AutoSize = false,
                    Size = new Size(300, 100),
                    Location = new Point(0, 100),
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                loadingPanel.Controls.Add(loadingLabel);

                // Thêm loading panel vào form
                var mainPanel = this.Controls[0] as Panel;
                if (mainPanel != null)
                {
                    mainPanel.Controls.Add(loadingPanel);
                    loadingPanel.BringToFront();
                }

                // Load QR code
                var bankCode = _paymentConfig.BankCode ?? "VCB";
                var qrImage = await QRCodeHelper.GenerateBankQRAsync(
                    bankCode,
                    _paymentConfig.AccountNumber,
                    _paymentConfig.AccountName,
                    _hoaDon.ConNo,
                    transferContent
                );

                // Hiển thị QR code
                _pbQRCode.Image = qrImage;

                // Xóa loading panel
                if (mainPanel != null && mainPanel.Controls.Contains(loadingPanel))
                {
                    mainPanel.Controls.Remove(loadingPanel);
                    loadingPanel.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin thanh toán: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private async void BtnConfirmPayment_Click(object sender, EventArgs e)
        {
            try
            {
                var confirmMessage = $"Xác nhận thanh toán hóa đơn?\n\n" +
                    $"📄 Hóa đơn: {_hoaDon.MaHoaDon}\n" +
                    $"💰 Số tiền: {_hoaDon.ConNo:N0} VNĐ\n\n" +
                    $"⚠️ Lưu ý:\n" +
                    $"• Vui lòng đảm bảo đã chuyển khoản ĐÚNG số tiền\n" +
                    $"• Nội dung chuyển khoản ĐÚNG như hướng dẫn\n" +
                    $"• Quản lý sẽ xác nhận trong vòng 24h\n\n" +
                    $"Bạn đã hoàn thành chuyển khoản chưa?";

                var confirm = MessageBox.Show(
                    confirmMessage,
                    "⚡ Xác nhận thanh toán",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirm != DialogResult.Yes)
                    return;

                _btnConfirmPayment.Enabled = false;
                _btnConfirmPayment.Text = "⏳ Đang xử lý...";
                _btnConfirmPayment.BackColor = Color.FromArgb(108, 117, 125);

                // Lưu thông tin thanh toán vào database
                var success = await ConfirmInvoicePaymentAsync();

                if (success)
                {
                    MessageBox.Show(
                        "✅ Đã ghi nhận yêu cầu thanh toán!\n\n" +
                        "📌 Quản lý sẽ kiểm tra và xác nhận thanh toán của bạn.\n" +
                        "🔔 Bạn sẽ nhận được thông báo khi thanh toán được xác nhận.\n\n" +
                        "⏱️ Thời gian xác nhận: Trong vòng 24 giờ",
                        "Thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    PaymentConfirmed = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        "❌ Có lỗi xảy ra khi xác nhận thanh toán.\n\n" +
                        "Vui lòng thử lại hoặc liên hệ quản lý.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    _btnConfirmPayment.Enabled = true;
                    _btnConfirmPayment.Text = "✓ Đã thanh toán";
                    _btnConfirmPayment.BackColor = Color.FromArgb(40, 167, 69);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Lỗi: {ex.Message}\n\nVui lòng thử lại.",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _btnConfirmPayment.Enabled = true;
                _btnConfirmPayment.Text = "✓ Đã thanh toán";
                _btnConfirmPayment.BackColor = Color.FromArgb(40, 167, 69);
            }
        }

        /// <summary>
        /// Ghi nhận thanh toán hóa đơn
        /// </summary>
        private async Task<bool> ConfirmInvoicePaymentAsync()
        {
            // Tạo bản ghi thanh toán chờ xác nhận
            // Sử dụng stored procedure hoặc update trực tiếp
            var repo = new HoaDonRepository();

            // Tạm thời đánh dấu là "Chờ xác nhận thanh toán"
            // Sau này admin sẽ xác nhận và cập nhật thành "Đã thanh toán"
            return await repo.MarkAsPendingPaymentConfirmationAsync(_hoaDon.HoaDonId, _tenantUserId);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(600, 750);
            this.Name = "InvoicePaymentDialog";
            this.ResumeLayout(false);
        }
    }
}
