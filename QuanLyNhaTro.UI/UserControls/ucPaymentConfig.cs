using QuanLyNhaTro.DAL;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;
using Dapper;

namespace QuanLyNhaTro.UI.UserControls
{
    /// <summary>
    /// Trang cấu hình thanh toán QR - Admin only
    /// </summary>
    public partial class ucPaymentConfig : UserControl
    {
        private readonly PaymentRepository _repo = new();

        // Controls
        private DataGridView dgvConfigs = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private Button btnSetDefault = null!;

        public ucPaymentConfig()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;
            this.Padding = new Padding(0);

            // ===== HEADER PANEL =====
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20, 15, 20, 15)
            };
            UIHelper.RoundControl(pnlHeader, 12);

            var lblTitle = new Label
            {
                Text = "💳 Cấu hình thanh toán VietQR",
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(0, 12),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "Quản lý thông tin tài khoản ngân hàng để nhận thanh toán cọc",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(0, 36),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblDesc });

            // ===== ACTION PANEL =====
            var pnlActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20, 10, 20, 10)
            };
            UIHelper.RoundControl(pnlActions, 12);

            btnAdd = new Button
            {
                Text = "➕ Thêm mới",
                Location = new Point(20, 12),
                Size = new Size(120, 38),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnAdd, 8);
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "✏️ Sửa",
                Location = new Point(150, 12),
                Size = new Size(100, 38),
                BackColor = ThemeManager.Warning,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnEdit, 8);
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "🗑️ Xóa",
                Location = new Point(260, 12),
                Size = new Size(100, 38),
                BackColor = ThemeManager.Error,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnDelete, 8);
            btnDelete.Click += BtnDelete_Click;

            var lblSeparator = new Label
            {
                Text = "|",
                Location = new Point(380, 18),
                ForeColor = ThemeManager.Border,
                AutoSize = true
            };

            btnSetDefault = new Button
            {
                Text = "⭐ Đặt làm mặc định",
                Location = new Point(410, 12),
                Size = new Size(150, 38),
                BackColor = ThemeManager.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnSetDefault.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnSetDefault, 8);
            btnSetDefault.Click += BtnSetDefault_Click;

            pnlActions.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, lblSeparator, btnSetDefault });

            // ===== DATA GRID =====
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };
            UIHelper.RoundControl(pnlGrid, 12);

            dgvConfigs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Surface,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            UIHelper.StyleDataGridView(dgvConfigs);
            SetupColumns();
            dgvConfigs.SelectionChanged += DgvConfigs_SelectionChanged;
            dgvConfigs.CellFormatting += DgvConfigs_CellFormatting;

            pnlGrid.Controls.Add(dgvConfigs);

            var spacer1 = new Panel { Dock = DockStyle.Top, Height = 15, BackColor = Color.Transparent };
            var spacer2 = new Panel { Dock = DockStyle.Bottom, Height = 10, BackColor = Color.Transparent };

            this.Controls.Add(pnlGrid);
            this.Controls.Add(spacer2);
            this.Controls.Add(pnlActions);
            this.Controls.Add(spacer1);
            this.Controls.Add(pnlHeader);
        }

        private void SetupColumns()
        {
            dgvConfigs.Columns.Clear();
            UIHelper.AddColumn(dgvConfigs, "ConfigId", "ID", "ConfigId", 50);
            UIHelper.AddColumn(dgvConfigs, "BankName", "Ngân hàng", "BankName", 150);
            UIHelper.AddColumn(dgvConfigs, "BankCode", "Mã NH", "BankCode", 80);
            UIHelper.AddColumn(dgvConfigs, "AccountNumber", "Số tài khoản", "AccountNumber", 140);
            UIHelper.AddColumn(dgvConfigs, "AccountName", "Chủ tài khoản", "AccountName", 200);
            UIHelper.AddColumn(dgvConfigs, "TransferTemplate", "Template CK", "TransferTemplate", 180);
            UIHelper.AddColumn(dgvConfigs, "DepositMonths", "Số tháng cọc", "DepositMonths", 100);
            UIHelper.AddColumn(dgvConfigs, "IsActive", "Mặc định", "IsActive", 80);
        }

        private async void LoadData()
        {
            try
            {
                var configs = await _repo.GetAllConfigsAsync();
                dgvConfigs.DataSource = configs.ToList();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void DgvConfigs_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = dgvConfigs.SelectedRows.Count > 0;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
            btnSetDefault.Enabled = hasSelection;
        }

        private void DgvConfigs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvConfigs.Columns[e.ColumnIndex].Name == "IsActive" && e.Value != null)
            {
                var isActive = (bool)e.Value;
                e.Value = isActive ? "⭐ Mặc định" : "";
                e.CellStyle.ForeColor = isActive ? ThemeManager.Success : ThemeManager.TextSecondary;
                e.CellStyle.Font = new Font("Segoe UI", 9, isActive ? FontStyle.Bold : FontStyle.Regular);
                e.FormattingApplied = true;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            ShowConfigForm(null);
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvConfigs.SelectedRows.Count == 0) return;
            var config = dgvConfigs.SelectedRows[0].DataBoundItem as PaymentConfig;
            ShowConfigForm(config);
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvConfigs.SelectedRows.Count == 0) return;
            var config = dgvConfigs.SelectedRows[0].DataBoundItem as PaymentConfig;
            if (config == null) return;

            if (config.IsActive)
            {
                UIHelper.ShowWarning("Không thể xóa cấu hình mặc định!\nVui lòng đặt cấu hình khác làm mặc định trước.");
                return;
            }

            var result = MessageBox.Show(
                $"Xác nhận xóa cấu hình ngân hàng:\n{config.BankName} - {config.AccountNumber}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                using var conn = DatabaseHelper.CreateConnection();
                await conn.ExecuteAsync("DELETE FROM PAYMENT_CONFIG WHERE ConfigId = @ConfigId", new { config.ConfigId });
                UIHelper.ShowSuccess("Đã xóa cấu hình thanh toán!");
                LoadData();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi xóa: {ex.Message}");
            }
        }

        private async void BtnSetDefault_Click(object? sender, EventArgs e)
        {
            if (dgvConfigs.SelectedRows.Count == 0) return;
            var config = dgvConfigs.SelectedRows[0].DataBoundItem as PaymentConfig;
            if (config == null) return;

            if (config.IsActive)
            {
                MessageBox.Show("Cấu hình này đã là mặc định!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var conn = DatabaseHelper.CreateConnection();
                // Unset all
                await conn.ExecuteAsync("UPDATE PAYMENT_CONFIG SET IsActive = 0");
                // Set selected
                await conn.ExecuteAsync("UPDATE PAYMENT_CONFIG SET IsActive = 1 WHERE ConfigId = @ConfigId", new { config.ConfigId });
                UIHelper.ShowSuccess($"Đã đặt {config.BankName} làm cấu hình mặc định!");
                LoadData();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi cập nhật: {ex.Message}");
            }
        }

        private void ShowConfigForm(PaymentConfig? config)
        {
            var isEdit = config != null;
            var popup = new Form
            {
                Text = isEdit ? "Sửa cấu hình thanh toán" : "Thêm cấu hình thanh toán",
                Size = new Size(550, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeManager.Background
            };

            int y = 20;

            // Bank Name
            AddFormRow(popup, "Tên ngân hàng:", out TextBox txtBankName, ref y, config?.BankName);

            // Bank Code
            var lblCode = new Label { Text = "Mã ngân hàng:", Location = new Point(20, y + 3), Width = 130, ForeColor = ThemeManager.TextPrimary };
            var cboBankCode = new ComboBox
            {
                Location = new Point(160, y),
                Size = new Size(150, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Thêm danh sách ngân hàng
            var banks = new Dictionary<string, string>
            {
                { "VCB - Vietcombank", "VCB" },
                { "TCB - Techcombank", "TCB" },
                { "MB - MB Bank", "MB" },
                { "VPB - VPBank", "VPB" },
                { "ACB - ACB", "ACB" },
                { "BIDV - BIDV", "BIDV" },
                { "VIB - VIB", "VIB" },
                { "TPB - TPBank", "TPB" },
                { "STB - Sacombank", "STB" },
                { "VTB - VietinBank", "VTB" },
                { "HDB - HDBank", "HDB" },
                { "SHB - SHB", "SHB" },
                { "MSB - MSB", "MSB" },
                { "OCB - OCB", "OCB" },
                { "SCB - SCB", "SCB" }
            };

            foreach (var bank in banks)
                cboBankCode.Items.Add(bank.Key);

            if (config != null && !string.IsNullOrEmpty(config.BankCode))
            {
                var selectedIndex = banks.Values.ToList().IndexOf(config.BankCode);
                if (selectedIndex >= 0)
                    cboBankCode.SelectedIndex = selectedIndex;
            }
            else
            {
                cboBankCode.SelectedIndex = 0;
            }

            var lblCodeHint = new Label
            {
                Text = "(Chọn từ danh sách)",
                Location = new Point(320, y + 3),
                ForeColor = ThemeManager.TextSecondary,
                AutoSize = true
            };

            popup.Controls.AddRange(new Control[] { lblCode, cboBankCode, lblCodeHint });
            y += 35;

            // Account Number
            AddFormRow(popup, "Số tài khoản:", out TextBox txtAccountNumber, ref y, config?.AccountNumber);

            // Account Name
            AddFormRow(popup, "Chủ tài khoản:", out TextBox txtAccountName, ref y, config?.AccountName);

            // Transfer Template
            var lblTemplate = new Label { Text = "Template CK:", Location = new Point(20, y + 3), Width = 130, ForeColor = ThemeManager.TextPrimary };
            var txtTemplate = new TextBox
            {
                Location = new Point(160, y),
                Size = new Size(340, 30),
                Text = config?.TransferTemplate ?? "NTPRO_{MaYeuCau}_{MaPhong}"
            };
            var lblTemplateHint = new Label
            {
                Text = "Sử dụng: {MaYeuCau}, {MaPhong}",
                Location = new Point(160, y + 28),
                ForeColor = ThemeManager.TextSecondary,
                AutoSize = true
            };
            popup.Controls.AddRange(new Control[] { lblTemplate, txtTemplate, lblTemplateHint });
            y += 60;

            // Deposit Months
            var lblDeposit = new Label { Text = "Số tháng cọc:", Location = new Point(20, y + 3), Width = 130, ForeColor = ThemeManager.TextPrimary };
            var nudDeposit = new NumericUpDown
            {
                Location = new Point(160, y),
                Size = new Size(80, 30),
                Minimum = 1,
                Maximum = 12,
                Value = config?.DepositMonths ?? 1
            };
            var lblDepositHint = new Label
            {
                Text = "tháng (tiền cọc = giá phòng x số tháng)",
                Location = new Point(250, y + 3),
                ForeColor = ThemeManager.TextSecondary,
                AutoSize = true
            };
            popup.Controls.AddRange(new Control[] { lblDeposit, nudDeposit, lblDepositHint });
            y += 40;

            // IsActive
            var chkIsActive = new CheckBox
            {
                Text = "Đặt làm cấu hình mặc định",
                Location = new Point(160, y),
                AutoSize = true,
                Checked = config?.IsActive ?? false
            };
            popup.Controls.Add(chkIsActive);
            y += 35;

            // Buttons
            var btnSave = new Button
            {
                Text = isEdit ? "💾 Lưu" : "➕ Thêm",
                Location = new Point(160, y),
                Size = new Size(120, 42),
                BackColor = ThemeManager.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnSave, 8);

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(290, y),
                Size = new Size(80, 42),
                BackColor = ThemeManager.Secondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            UIHelper.RoundControl(btnCancel, 8);
            btnCancel.Click += (s, args) => popup.Close();

            btnSave.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtBankName.Text))
                {
                    UIHelper.ShowWarning("Vui lòng nhập tên ngân hàng!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAccountNumber.Text))
                {
                    UIHelper.ShowWarning("Vui lòng nhập số tài khoản!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAccountName.Text))
                {
                    UIHelper.ShowWarning("Vui lòng nhập chủ tài khoản!");
                    return;
                }

                try
                {
                    var selectedBankCode = banks.ElementAt(cboBankCode.SelectedIndex).Value;

                    using var conn = DatabaseHelper.CreateConnection();

                    if (chkIsActive.Checked)
                    {
                        // Unset all
                        await conn.ExecuteAsync("UPDATE PAYMENT_CONFIG SET IsActive = 0");
                    }

                    if (isEdit)
                    {
                        await conn.ExecuteAsync(@"
                            UPDATE PAYMENT_CONFIG SET
                                BankName = @BankName,
                                BankCode = @BankCode,
                                AccountNumber = @AccountNumber,
                                AccountName = @AccountName,
                                TransferTemplate = @TransferTemplate,
                                DepositMonths = @DepositMonths,
                                IsActive = @IsActive
                            WHERE ConfigId = @ConfigId",
                            new
                            {
                                config!.ConfigId,
                                BankName = txtBankName.Text.Trim(),
                                BankCode = selectedBankCode,
                                AccountNumber = txtAccountNumber.Text.Trim(),
                                AccountName = txtAccountName.Text.Trim(),
                                TransferTemplate = txtTemplate.Text.Trim(),
                                DepositMonths = (int)nudDeposit.Value,
                                IsActive = chkIsActive.Checked
                            });
                        UIHelper.ShowSuccess("Đã cập nhật cấu hình!");
                    }
                    else
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO PAYMENT_CONFIG (BankName, BankCode, AccountNumber, AccountName, TransferTemplate, DepositMonths, IsActive)
                            VALUES (@BankName, @BankCode, @AccountNumber, @AccountName, @TransferTemplate, @DepositMonths, @IsActive)",
                            new
                            {
                                BankName = txtBankName.Text.Trim(),
                                BankCode = selectedBankCode,
                                AccountNumber = txtAccountNumber.Text.Trim(),
                                AccountName = txtAccountName.Text.Trim(),
                                TransferTemplate = txtTemplate.Text.Trim(),
                                DepositMonths = (int)nudDeposit.Value,
                                IsActive = chkIsActive.Checked
                            });
                        UIHelper.ShowSuccess("Đã thêm cấu hình thanh toán!");
                    }

                    popup.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi lưu: {ex.Message}");
                }
            };

            popup.Controls.AddRange(new Control[] { btnSave, btnCancel });
            popup.ShowDialog();
        }

        private void AddFormRow(Form form, string label, out TextBox textBox, ref int y, string? defaultValue = null)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(20, y + 3),
                Width = 130,
                ForeColor = ThemeManager.TextPrimary
            };

            textBox = new TextBox
            {
                Location = new Point(160, y),
                Size = new Size(340, 30),
                Text = defaultValue ?? ""
            };

            form.Controls.AddRange(new Control[] { lbl, textBox });
            y += 35;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucPaymentConfig";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }
}
