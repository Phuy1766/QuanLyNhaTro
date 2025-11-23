using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;
using System.Drawing;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyContract : UserControl
    {
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _tenantUserId;

        private DataGridView dgvContracts = null!;
        private Panel pnlDetail = null!;
        private Panel pnlDetailContent = null!;
        private Label lblNoSelection = null!;
        private HopDong? _selectedContract;

        public ucMyContract(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            CreateLayout();
            LoadContractsAsync();
        }

        private void CreateLayout()
        {
            this.BackColor = ColorTranslator.FromHtml("#F3F4F6");
            this.Padding = new Padding(20);

            // Main layout: 40% list | 60% detail
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 40F),
                    new ColumnStyle(SizeType.Percent, 60F)
                }
            };

            // Left panel: Danh sách hợp đồng
            var pnlList = CreateListPanel();
            mainLayout.Controls.Add(pnlList, 0, 0);

            // Right panel: Chi tiết
            pnlDetail = CreateDetailPanel();
            mainLayout.Controls.Add(pnlDetail, 1, 0);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateListPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 10, 0)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "Lịch sử hợp đồng",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1F2937"),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            panel.Controls.Add(lblTitle);

            // DataGridView
            dgvContracts = new DataGridView
            {
                Location = new Point(0, 35),
                Width = panel.Width,
                Height = panel.Height - 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 60 }
            };

            // Style
            dgvContracts.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F9FAFB");
            dgvContracts.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#374151");
            dgvContracts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvContracts.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvContracts.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);

            dgvContracts.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
            dgvContracts.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#1E40AF");
            dgvContracts.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvContracts.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            dgvContracts.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            dgvContracts.SelectionChanged += DgvContracts_SelectionChanged;
            dgvContracts.CellFormatting += DgvContracts_CellFormatting;

            SetupListColumns();

            panel.Controls.Add(dgvContracts);

            return panel;
        }

        private void SetupListColumns()
        {
            dgvContracts.Columns.Clear();

            dgvContracts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaHopDong",
                HeaderText = "Mã HĐ",
                DataPropertyName = "MaHopDong",
                Width = 90,
                MinimumWidth = 80
            });

            dgvContracts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaPhong",
                HeaderText = "Phòng",
                DataPropertyName = "MaPhong",
                Width = 70,
                MinimumWidth = 60
            });

            dgvContracts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NgayBatDau",
                HeaderText = "Bắt đầu",
                DataPropertyName = "NgayBatDau",
                Width = 90,
                MinimumWidth = 80,
                DefaultCellStyle = { Format = "dd/MM/yyyy" }
            });

            dgvContracts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TrangThai",
                HeaderText = "Trạng thái",
                DataPropertyName = "TrangThai",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }

        private void DgvContracts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvContracts.Columns[e.ColumnIndex].Name == "TrangThai" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                e.Value = GetStatusDisplayText(status);
                e.CellStyle.ForeColor = GetStatusColor(status);
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
        }

        private string GetStatusDisplayText(string status)
        {
            return status switch
            {
                "Active" => "Đang hiệu lực",
                "Terminated" => "Đã thanh lý",
                "Expired" => "Đã hết hạn",
                _ => status
            };
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "Active" => ColorTranslator.FromHtml("#10B981"),
                "Terminated" => ColorTranslator.FromHtml("#6B7280"),
                "Expired" => ColorTranslator.FromHtml("#EF4444"),
                _ => ColorTranslator.FromHtml("#6B7280")
            };
        }

        private Panel CreateDetailPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0)
            };

            var innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle
            };

            // No selection label
            lblNoSelection = new Label
            {
                Text = "Chọn một hợp đồng để xem chi tiết",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = ColorTranslator.FromHtml("#9CA3AF"),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Detail content panel
            pnlDetailContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Visible = false,
                Padding = new Padding(30, 25, 30, 25)
            };

            innerPanel.Controls.Add(lblNoSelection);
            innerPanel.Controls.Add(pnlDetailContent);

            panel.Controls.Add(innerPanel);

            return panel;
        }

        private void DgvContracts_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvContracts.SelectedRows.Count > 0)
            {
                var row = dgvContracts.SelectedRows[0];
                int hopDongId = Convert.ToInt32(row.Cells["HopDongId"].Value);
                LoadDetailPanel(hopDongId);
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
            _selectedContract = null;
        }

        private async void LoadContractsAsync()
        {
            try
            {
                var contracts = (await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId))
                    .OrderByDescending(c => c.NgayBatDau)
                    .ToList();

                // Add HopDongId as hidden column
                if (!dgvContracts.Columns.Contains("HopDongId"))
                {
                    dgvContracts.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "HopDongId",
                        DataPropertyName = "HopDongId",
                        Visible = false
                    });
                }

                dgvContracts.DataSource = contracts;

                if (dgvContracts.Rows.Count == 0)
                {
                    ShowNoSelection();
                    lblNoSelection.Text = "Bạn chưa có hợp đồng nào";
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải danh sách hợp đồng: {ex.Message}");
            }
        }

        private void LoadDetailPanel(int hopDongId)
        {
            try
            {
                _selectedContract = dgvContracts.DataSource is List<HopDong> list
                    ? list.FirstOrDefault(c => c.HopDongId == hopDongId)
                    : null;

                if (_selectedContract == null)
                {
                    ShowNoSelection();
                    return;
                }

                pnlDetailContent.Controls.Clear();

                int yPos = 0;

                // Header
                var lblHeader = new Label
                {
                    Text = $"Chi tiết hợp đồng {_selectedContract.MaHopDong}",
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = ColorTranslator.FromHtml("#1F2937"),
                    AutoSize = true,
                    Location = new Point(0, yPos)
                };
                pnlDetailContent.Controls.Add(lblHeader);
                yPos += 45;

                // Thông tin cơ bản
                var block1 = CreateInfoBlock("Thông tin hợp đồng", new[]
                {
                    new InfoItem("Mã hợp đồng", _selectedContract.MaHopDong ?? "N/A"),
                    new InfoItem("Phòng", _selectedContract.MaPhong ?? "N/A"),
                    new InfoItem("Tòa nhà", _selectedContract.BuildingName ?? "N/A"),
                    new InfoItem("Ngày bắt đầu", _selectedContract.NgayBatDau.ToString("dd/MM/yyyy")),
                    new InfoItem("Ngày kết thúc", _selectedContract.NgayKetThuc.ToString("dd/MM/yyyy")),
                    new InfoItem("Trạng thái", GetStatusDisplayText(_selectedContract.TrangThai ?? ""))
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block1);
                yPos += block1.Height + 20;

                // Thông tin tài chính
                var block2 = CreateInfoBlock("Thông tin tài chính", new[]
                {
                    new InfoItem("Giá thuê", $"{_selectedContract.GiaThue:N0} VNĐ/tháng"),
                    new InfoItem("Tiền cọc", $"{_selectedContract.TienCoc:N0} VNĐ"),
                    new InfoItem("Chu kỳ thanh toán", $"{_selectedContract.ChuKyThanhToan} tháng"),
                    new InfoItem("Tiền hoàn cọc", _selectedContract.TienHoanCoc.HasValue ? $"{_selectedContract.TienHoanCoc.Value:N0} VNĐ" : "Chưa có"),
                    new InfoItem("Tiền khấu trừ", _selectedContract.TienKhauTru.HasValue ? $"{_selectedContract.TienKhauTru.Value:N0} VNĐ" : "Không có")
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block2);
                yPos += block2.Height + 20;

                // Thời gian còn lại (nếu Active)
                if (_selectedContract.TrangThai == "Active")
                {
                    var daysRemaining = (_selectedContract.NgayKetThuc - DateTime.Now).Days;
                    string statusText;
                    Color statusColor;

                    if (daysRemaining < 0)
                    {
                        statusText = "Đã hết hạn";
                        statusColor = ColorTranslator.FromHtml("#EF4444");
                    }
                    else if (daysRemaining <= 30)
                    {
                        statusText = $"Sắp hết hạn - còn {daysRemaining} ngày";
                        statusColor = ColorTranslator.FromHtml("#F59E0B");
                    }
                    else
                    {
                        statusText = $"Còn hiệu lực - còn {daysRemaining} ngày";
                        statusColor = ColorTranslator.FromHtml("#10B981");
                    }

                    var lblStatus = new Label
                    {
                        Text = statusText,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = statusColor,
                        AutoSize = true,
                        Location = new Point(0, yPos),
                        Padding = new Padding(10, 8, 10, 8),
                        BackColor = Color.FromArgb(20, statusColor)
                    };
                    pnlDetailContent.Controls.Add(lblStatus);
                    yPos += 40;
                }

                // Ghi chú và lý do khấu trừ
                if (!string.IsNullOrEmpty(_selectedContract.GhiChu) || !string.IsNullOrEmpty(_selectedContract.LyDoKhauTru))
                {
                    var items = new List<InfoItem>();
                    if (!string.IsNullOrEmpty(_selectedContract.GhiChu))
                        items.Add(new InfoItem("Ghi chú", _selectedContract.GhiChu));
                    if (!string.IsNullOrEmpty(_selectedContract.LyDoKhauTru))
                        items.Add(new InfoItem("Lý do khấu trừ", _selectedContract.LyDoKhauTru));

                    var block3 = CreateInfoBlock("Thông tin bổ sung", items.ToArray(), 0, yPos);
                    pnlDetailContent.Controls.Add(block3);
                    yPos += block3.Height + 20;
                }

                // Ngày thanh lý (nếu có)
                if (_selectedContract.NgayThanhLy.HasValue)
                {
                    var block4 = CreateInfoBlock("Thông tin thanh lý", new[]
                    {
                        new InfoItem("Ngày thanh lý", _selectedContract.NgayThanhLy.Value.ToString("dd/MM/yyyy HH:mm")),
                        new InfoItem("Tiền hoàn cọc", _selectedContract.TienHoanCoc.HasValue ? $"{_selectedContract.TienHoanCoc.Value:N0} VNĐ" : "0 VNĐ"),
                        new InfoItem("Tiền khấu trừ", _selectedContract.TienKhauTru.HasValue ? $"{_selectedContract.TienKhauTru.Value:N0} VNĐ" : "0 VNĐ")
                    }, 0, yPos);
                    pnlDetailContent.Controls.Add(block4);
                }

                lblNoSelection.Visible = false;
                pnlDetailContent.Visible = true;
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải chi tiết: {ex.Message}");
            }
        }

        private Panel CreateInfoBlock(string title, InfoItem[] items, int x, int y)
        {
            int blockWidth = 500;
            if (pnlDetailContent.ClientSize.Width > 100)
            {
                blockWidth = pnlDetailContent.ClientSize.Width - pnlDetailContent.Padding.Left - pnlDetailContent.Padding.Right - 20;
            }

            var block = new Panel
            {
                Location = new Point(x, y),
                Width = blockWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ColorTranslator.FromHtml("#F9FAFB"),
                Padding = new Padding(20),
                AutoSize = false
            };

            int blockY = 0;

            var lblTitle = new Label
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
                var lblLabel = new Label
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

                var lblValue = new Label
                {
                    Text = item.Value,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = ColorTranslator.FromHtml("#1F2937"),
                    AutoSize = false,
                    Width = blockWidth - 240,
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

            return block;
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
            this.Name = "ucMyContract";
            this.Size = new Size(1100, 700);
        }
    }
}
