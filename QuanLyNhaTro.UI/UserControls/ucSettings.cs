using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucSettings : UserControl
    {
        private readonly CauHinhRepository _configRepo = new();
        private readonly EmailService _emailService = new();
        private readonly PaymentRepository _paymentRepo = new();

        private Label _lblTitle = null!;
        private Label _lblDesc = null!;

        private Panel _pnlInfo = null!;
        private Panel _pnlPrice = null!;
        private Panel _pnlSmtp = null!;
        private Panel? _pnlPayment;

        private Button _btnSave = null!;

        // inputs
        private TextBox txtTenNhaTro = null!;
        private TextBox txtDiaChi = null!;
        private TextBox txtSDT = null!;
        private TextBox txtEmail = null!;

        private NumericUpDown nudGiaDien = null!;
        private NumericUpDown nudGiaNuoc = null!;

        private TextBox txtSmtpHost = null!;
        private TextBox txtSmtpPort = null!;
        private TextBox txtSmtpEmail = null!;
        private TextBox txtSmtpPassword = null!;

        private TextBox txtBankName = null!;
        private TextBox txtAccountNumber = null!;
        private TextBox txtAccountName = null!;
        private TextBox txtTransferTemplate = null!;
        private ComboBox cboBankCode = null!;
        private NumericUpDown nudDepositMonths = null!;
        private int? _currentPaymentConfigId;

        public ucSettings()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Name = "ucSettings";
            this.Size = new Size(1200, 720);
        }

        #region LAYOUT

        private void CreateLayout()
        {
            this.BackColor = Color.FromArgb(243, 244, 246); // #F3F4F6
            this.AutoScroll = true;

            // ==== header ====
            _lblTitle = new Label
            {
                Text = "Cài đặt hệ thống",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                AutoSize = true
            };
            this.Controls.Add(_lblTitle);

            _lblDesc = new Label
            {
                Text = "Quản lý cấu hình chung cho hệ thống Nhà Trọ Pro",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true
            };
            this.Controls.Add(_lblDesc);

            int sectionHeightInfo = 220;
            int sectionHeightPrice = 160;
            int sectionHeightSmtp = 260;
            int sectionHeightPayment = 320;   // tăng cao hơn để không bị mất dòng

            // ==== Thông tin nhà trọ ====
            _pnlInfo = CreateSectionPanel("Thông tin nhà trọ", sectionHeightInfo);
            int y = 60;
            AddRow(_pnlInfo, "Tên nhà trọ:", ref txtTenNhaTro, ref y);
            AddRow(_pnlInfo, "Địa chỉ:", ref txtDiaChi, ref y);
            AddRow(_pnlInfo, "SĐT liên hệ:", ref txtSDT, ref y);
            AddRow(_pnlInfo, "Email:", ref txtEmail, ref y);
            this.Controls.Add(_pnlInfo);

            // ==== Giá dịch vụ ====
            _pnlPrice = CreateSectionPanel("Giá dịch vụ mặc định", sectionHeightPrice);
            y = 60;
            AddLabel(_pnlPrice, "Giá điện (VNĐ/kWh):", y);
            nudGiaDien = new NumericUpDown
            {
                Location = new Point(180, y - 3),
                Size = new Size(200, 25),
                Maximum = 100000,
                ThousandsSeparator = true
            };
            _pnlPrice.Controls.Add(nudGiaDien);
            y += 40;

            AddLabel(_pnlPrice, "Giá nước (VNĐ/m³):", y);
            nudGiaNuoc = new NumericUpDown
            {
                Location = new Point(180, y - 3),
                Size = new Size(200, 25),
                Maximum = 100000,
                ThousandsSeparator = true
            };
            _pnlPrice.Controls.Add(nudGiaNuoc);
            this.Controls.Add(_pnlPrice);

            // ==== SMTP ====
            _pnlSmtp = CreateSectionPanel("Cấu hình gửi email (SMTP Gmail)", sectionHeightSmtp);
            y = 60;
            AddRow(_pnlSmtp, "SMTP Host:", ref txtSmtpHost, ref y);
            AddRow(_pnlSmtp, "SMTP Port:", ref txtSmtpPort, ref y);
            AddRow(_pnlSmtp, "Email gửi:", ref txtSmtpEmail, ref y);

            AddLabel(_pnlSmtp, "App Password:", y);
            txtSmtpPassword = new TextBox
            {
                Location = new Point(180, y - 3),
                Size = new Size(350, 25),
                PasswordChar = '*'
            };
            _pnlSmtp.Controls.Add(txtSmtpPassword);
            y += 40;

            var btnTest = new Button
            {
                Text = "🔌 Test kết nối",
                Size = new Size(140, 32),
                Location = new Point(180, y),
                BackColor = Color.FromArgb(168, 85, 247),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += BtnTest_Click;
            _pnlSmtp.Controls.Add(btnTest);
            this.Controls.Add(_pnlSmtp);

            // ==== VietQR (Admin) ====
            if (AuthService.CurrentUser?.RoleName == "Admin")
            {
                _pnlPayment = CreateSectionPanel("Cấu hình thanh toán VietQR", sectionHeightPayment);
                y = 60;

                AddRow(_pnlPayment, "Tên ngân hàng:", ref txtBankName, ref y);

                AddLabel(_pnlPayment, "Mã ngân hàng:", y);
                cboBankCode = new ComboBox
                {
                    Location = new Point(180, y - 3),
                    Size = new Size(220, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                var banks = new[]
                {
                    "VCB - Vietcombank", "TCB - Techcombank", "MB - MB Bank", "VPB - VPBank", "ACB - ACB",
                    "BIDV - BIDV", "VIB - VIB", "TPB - TPBank", "STB - Sacombank", "VTB - VietinBank",
                    "HDB - HDBank", "SHB - SHB", "MSB - MSB", "OCB - OCB", "SCB - SCB"
                };
                cboBankCode.Items.AddRange(banks);
                cboBankCode.SelectedIndex = 0;
                _pnlPayment.Controls.Add(cboBankCode);
                y += 40;

                AddRow(_pnlPayment, "Số tài khoản:", ref txtAccountNumber, ref y);
                AddRow(_pnlPayment, "Chủ tài khoản:", ref txtAccountName, ref y);
                AddRow(_pnlPayment, "Template CK:", ref txtTransferTemplate, ref y);

                AddLabel(_pnlPayment, "Số tháng cọc:", y);
                nudDepositMonths = new NumericUpDown
                {
                    Location = new Point(180, y - 3),
                    Size = new Size(80, 25),
                    Minimum = 1,
                    Maximum = 12,
                    Value = 1
                };
                _pnlPayment.Controls.Add(nudDepositMonths);

                this.Controls.Add(_pnlPayment);
            }

            // ==== Nút lưu ====
            _btnSave = new Button
            {
                Text = "💾 Lưu cấu hình",
                Size = new Size(170, 44),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;
            this.Controls.Add(_btnSave);

            // layout lần đầu + khi resize
            this.Resize += (_, __) => ApplyGridLayout();
            ApplyGridLayout();
        }

        private Panel CreateSectionPanel(string title, int height)
        {
            var pnl = new Panel
            {
                Height = height,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(20, 18),
                AutoSize = true
            };
            pnl.Controls.Add(lblTitle);

            var underline = new Panel
            {
                BackColor = Color.FromArgb(229, 231, 235),
                Location = new Point(20, 42),
                Size = new Size(640, 1),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnl.Controls.Add(underline);

            return pnl;
        }

        private void AddRow(Panel panel, string label, ref TextBox txt, ref int y)
        {
            AddLabel(panel, label, y);
            txt = new TextBox
            {
                Location = new Point(180, y - 3),
                Size = new Size(400, 25)
            };
            panel.Controls.Add(txt);
            y += 40;
        }

        private void AddLabel(Panel panel, string text, int y)
        {
            panel.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(20, y),
                AutoSize = true
            });
        }

        /// <summary>
        /// Bố cục 2x2, nút lưu xuống dưới cùng
        /// </summary>
        private void ApplyGridLayout()
        {
            if (this.ClientSize.Width <= 0) return;

            int sidePadding = 40;
            int columnGap = 24;
            int rowGap = 24;
            int bottomPadding = 32;

            int contentWidth = this.ClientSize.Width - sidePadding * 2;
            if (contentWidth < 700) contentWidth = this.ClientSize.Width - 20;

            int colWidth = (contentWidth - columnGap) / 2;
            int leftX = (this.ClientSize.Width - contentWidth) / 2;
            int rightX = leftX + colWidth + columnGap;

            // header
            _lblTitle.Location = new Point(leftX, 16);
            _lblDesc.Location = new Point(leftX, _lblTitle.Bottom + 4);

            int currentTop = _lblDesc.Bottom + 24;

            // row1
            _pnlInfo.Width = colWidth;
            _pnlInfo.Location = new Point(leftX, currentTop);

            _pnlPrice.Width = colWidth;
            _pnlPrice.Location = new Point(rightX, currentTop);

            int row1Bottom = Math.Max(_pnlInfo.Bottom, _pnlPrice.Bottom);
            currentTop = row1Bottom + rowGap;

            // row2
            _pnlSmtp.Width = colWidth;
            _pnlSmtp.Location = new Point(leftX, currentTop);

            if (_pnlPayment != null)
            {
                _pnlPayment.Width = colWidth;
                _pnlPayment.Location = new Point(rightX, currentTop);

                int row2Bottom = Math.Max(_pnlSmtp.Bottom, _pnlPayment.Bottom);
                currentTop = row2Bottom + rowGap;
            }
            else
            {
                currentTop = _pnlSmtp.Bottom + rowGap;
            }

            // vị trí mong muốn của nút save: đáy form
            int desiredTop = this.ClientSize.Height - _btnSave.Height - bottomPadding;

            // nhưng nếu form thấp, không để nút đè lên panel =>
            int finalTop = Math.Max(currentTop, desiredTop);

            _btnSave.Location = new Point(
                rightX + colWidth - _btnSave.Width,   // căn phải cột bên phải
                finalTop
            );
        }

        #endregion

        #region DATA

        private async void LoadData()
        {
            try
            {
                var configs = (await _configRepo.GetAllAsync())
                    .ToDictionary(c => c.MaCauHinh, c => c.GiaTri ?? "");

                txtTenNhaTro.Text = configs.GetValueOrDefault("TEN_NHA_TRO", "");
                txtDiaChi.Text = configs.GetValueOrDefault("DIA_CHI", "");
                txtSDT.Text = configs.GetValueOrDefault("SDT_LIEN_HE", "");
                txtEmail.Text = configs.GetValueOrDefault("EMAIL", "");

                nudGiaDien.Value = decimal.TryParse(configs.GetValueOrDefault("GIA_DIEN_MAC_DINH", "3500"), out var gd) ? gd : 3500;
                nudGiaNuoc.Value = decimal.TryParse(configs.GetValueOrDefault("GIA_NUOC_MAC_DINH", "15000"), out var gn) ? gn : 15000;

                txtSmtpHost.Text = configs.GetValueOrDefault("SMTP_HOST", "smtp.gmail.com");
                txtSmtpPort.Text = configs.GetValueOrDefault("SMTP_PORT", "587");
                txtSmtpEmail.Text = configs.GetValueOrDefault("SMTP_EMAIL", "");
                txtSmtpPassword.Text = configs.GetValueOrDefault("SMTP_PASSWORD", "");

                if (AuthService.CurrentUser?.RoleName == "Admin" && _pnlPayment != null)
                {
                    var paymentConfig = await _paymentRepo.GetDefaultConfigAsync();
                    if (paymentConfig != null)
                    {
                        _currentPaymentConfigId = paymentConfig.ConfigId;
                        txtBankName.Text = paymentConfig.BankName;
                        txtAccountNumber.Text = paymentConfig.AccountNumber;
                        txtAccountName.Text = paymentConfig.AccountName;
                        txtTransferTemplate.Text = paymentConfig.TransferTemplate;
                        nudDepositMonths.Value = paymentConfig.DepositMonths;

                        var bankIndex = cboBankCode.Items.Cast<string>()
                            .ToList()
                            .FindIndex(b => b.StartsWith(paymentConfig.BankCode + " "));
                        if (bankIndex >= 0)
                            cboBankCode.SelectedIndex = bankIndex;
                    }
                    else
                    {
                        txtTransferTemplate.Text = "NTPRO_{MaYeuCau}_{MaPhong}";
                        nudDepositMonths.Value = 1;
                    }
                }

                ApplyGridLayout();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải cấu hình: {ex.Message}");
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var configs = new Dictionary<string, string>
                {
                    ["TEN_NHA_TRO"] = txtTenNhaTro.Text,
                    ["DIA_CHI"] = txtDiaChi.Text,
                    ["SDT_LIEN_HE"] = txtSDT.Text,
                    ["EMAIL"] = txtEmail.Text,
                    ["GIA_DIEN_MAC_DINH"] = nudGiaDien.Value.ToString(),
                    ["GIA_NUOC_MAC_DINH"] = nudGiaNuoc.Value.ToString(),
                    ["SMTP_HOST"] = txtSmtpHost.Text,
                    ["SMTP_PORT"] = txtSmtpPort.Text,
                    ["SMTP_EMAIL"] = txtSmtpEmail.Text,
                    ["SMTP_PASSWORD"] = txtSmtpPassword.Text
                };

                await _configRepo.UpdateManyAsync(configs);

                if (AuthService.CurrentUser?.RoleName == "Admin" && _pnlPayment != null)
                {
                    await SavePaymentConfig();
                }

                UIHelper.ShowSuccess("Lưu cấu hình thành công!");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private async Task SavePaymentConfig()
        {
            if (string.IsNullOrWhiteSpace(txtBankName.Text) || string.IsNullOrWhiteSpace(txtAccountNumber.Text))
                return;

            using var conn = DAL.DatabaseHelper.CreateConnection();

            var selectedBank = cboBankCode.SelectedItem?.ToString() ?? "VCB - Vietcombank";
            var bankCode = selectedBank.Split(' ')[0];

            if (_currentPaymentConfigId.HasValue)
            {
                await conn.ExecuteAsync(@"
                    UPDATE PAYMENT_CONFIG SET
                        BankName = @BankName,
                        BankCode = @BankCode,
                        AccountNumber = @AccountNumber,
                        AccountName = @AccountName,
                        TransferTemplate = @TransferTemplate,
                        DepositMonths = @DepositMonths,
                        IsActive = 1
                    WHERE ConfigId = @ConfigId",
                    new
                    {
                        ConfigId = _currentPaymentConfigId.Value,
                        BankName = txtBankName.Text.Trim(),
                        BankCode = bankCode,
                        AccountNumber = txtAccountNumber.Text.Trim(),
                        AccountName = txtAccountName.Text.Trim(),
                        TransferTemplate = txtTransferTemplate.Text.Trim(),
                        DepositMonths = (int)nudDepositMonths.Value
                    });
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE PAYMENT_CONFIG SET IsActive = 0;

                    INSERT INTO PAYMENT_CONFIG
                        (BankName, BankCode, AccountNumber, AccountName, TransferTemplate, DepositMonths, IsActive)
                    VALUES
                        (@BankName, @BankCode, @AccountNumber, @AccountName, @TransferTemplate, @DepositMonths, 1)",
                    new
                    {
                        BankName = txtBankName.Text.Trim(),
                        BankCode = bankCode,
                        AccountNumber = txtAccountNumber.Text.Trim(),
                        AccountName = txtAccountName.Text.Trim(),
                        TransferTemplate = txtTransferTemplate.Text.Trim(),
                        DepositMonths = (int)nudDepositMonths.Value
                    });
            }
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            await _configRepo.UpdateAsync("SMTP_HOST", txtSmtpHost.Text);
            await _configRepo.UpdateAsync("SMTP_PORT", txtSmtpPort.Text);
            await _configRepo.UpdateAsync("SMTP_EMAIL", txtSmtpEmail.Text);
            await _configRepo.UpdateAsync("SMTP_PASSWORD", txtSmtpPassword.Text);

            var (ok, msg) = await _emailService.TestConnectionAsync();
            if (ok) UIHelper.ShowSuccess(msg);
            else UIHelper.ShowError(msg);
        }

        #endregion
    }
}
