using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;
using System.Drawing;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyRoom : UserControl
    {
        private readonly PhongTroRepository _phongRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly TaiSanRepository _taiSanRepo = new();
        private readonly int _tenantUserId;

        private DataGridView dgvRooms = null!;
        private Panel pnlDetail = null!;
        private Panel pnlDetailContent = null!;
        private Label lblNoSelection = null!;
        private HopDong? _selectedContract;

        public ucMyRoom(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            CreateLayout();
            LoadRoomsAsync();
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

            // Left panel: Danh sách phòng
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
                Text = "Danh sách phòng của bạn",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1F2937"),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            panel.Controls.Add(lblTitle);

            // DataGridView
            dgvRooms = new DataGridView
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
                RowTemplate = { Height = 70 }
            };

            // Style
            dgvRooms.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F9FAFB");
            dgvRooms.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#374151");
            dgvRooms.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvRooms.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvRooms.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);

            dgvRooms.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
            dgvRooms.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#1E40AF");
            dgvRooms.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvRooms.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            dgvRooms.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            dgvRooms.SelectionChanged += DgvRooms_SelectionChanged;

            SetupListColumns();

            panel.Controls.Add(dgvRooms);

            return panel;
        }

        private void SetupListColumns()
        {
            dgvRooms.Columns.Clear();

            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaPhong",
                HeaderText = "Mã phòng",
                DataPropertyName = "MaPhong",
                Width = 80,
                MinimumWidth = 80
            });

            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BuildingName",
                HeaderText = "Tòa nhà",
                DataPropertyName = "BuildingName",
                Width = 100,
                MinimumWidth = 90
            });

            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TenLoai",
                HeaderText = "Loại phòng",
                DataPropertyName = "TenLoai",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
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
                Text = "Chọn một phòng để xem chi tiết",
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

        private void DgvRooms_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvRooms.SelectedRows.Count > 0)
            {
                var row = dgvRooms.SelectedRows[0];
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

        private async void LoadRoomsAsync()
        {
            try
            {
                var contracts = (await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId))
                    .Where(c => c.TrangThai == "Active")
                    .OrderBy(c => c.MaPhong)
                    .ToList();

                // Add HopDongId as hidden column
                if (!dgvRooms.Columns.Contains("HopDongId"))
                {
                    dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "HopDongId",
                        DataPropertyName = "HopDongId",
                        Visible = false
                    });
                }

                dgvRooms.DataSource = contracts;

                if (dgvRooms.Rows.Count == 0)
                {
                    ShowNoSelection();
                    lblNoSelection.Text = "Bạn chưa có phòng nào đang thuê";
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải danh sách phòng: {ex.Message}");
            }
        }

        private async void LoadDetailPanel(int hopDongId)
        {
            try
            {
                var contracts = await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId);
                _selectedContract = contracts.FirstOrDefault(c => c.HopDongId == hopDongId);

                if (_selectedContract == null)
                {
                    ShowNoSelection();
                    return;
                }

                var room = await _phongRepo.GetByMaPhongAsync(_selectedContract.MaPhong);
                if (room == null)
                {
                    ShowNoSelection();
                    lblNoSelection.Text = "Không tìm thấy thông tin phòng";
                    return;
                }

                pnlDetailContent.Controls.Clear();

                int yPos = 0;

                // Header
                var lblHeader = new Label
                {
                    Text = $"Chi tiết phòng {room.MaPhong}",
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = ColorTranslator.FromHtml("#1F2937"),
                    AutoSize = true,
                    Location = new Point(0, yPos)
                };
                pnlDetailContent.Controls.Add(lblHeader);
                yPos += 45;

                // Thông tin phòng
                var block1 = CreateInfoBlock("Thông tin phòng", new[]
                {
                    new InfoItem("Mã phòng", room.MaPhong ?? "N/A"),
                    new InfoItem("Tòa nhà", room.BuildingName ?? "N/A"),
                    new InfoItem("Loại phòng", room.TenLoai ?? "N/A"),
                    new InfoItem("Tầng", room.Tang.ToString()),
                    new InfoItem("Diện tích", $"{room.DienTich} m²"),
                    new InfoItem("Giá thuê", $"{room.GiaThue:N0} VNĐ/tháng"),
                    new InfoItem("Số người tối đa", room.SoNguoiToiDa.ToString()),
                    new InfoItem("Trạng thái", room.TrangThai ?? "N/A")
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block1);
                yPos += block1.Height + 20;

                // Thông tin hợp đồng
                var block2 = CreateInfoBlock("Thông tin hợp đồng", new[]
                {
                    new InfoItem("Mã hợp đồng", _selectedContract.MaHopDong ?? "N/A"),
                    new InfoItem("Ngày bắt đầu", _selectedContract.NgayBatDau.ToString("dd/MM/yyyy")),
                    new InfoItem("Ngày kết thúc", _selectedContract.NgayKetThuc.ToString("dd/MM/yyyy")),
                    new InfoItem("Giá thuê", $"{_selectedContract.GiaThue:N0} VNĐ/tháng"),
                    new InfoItem("Tiền cọc", $"{_selectedContract.TienCoc:N0} VNĐ"),
                    new InfoItem("Chu kỳ thanh toán", $"{_selectedContract.ChuKyThanhToan} tháng"),
                    new InfoItem("Trạng thái", _selectedContract.TrangThai ?? "N/A")
                }, 0, yPos);
                pnlDetailContent.Controls.Add(block2);
                yPos += block2.Height + 20;

                // Tài sản trong phòng
                var lblAssets = new Label
                {
                    Text = "Tài sản trong phòng",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = ColorTranslator.FromHtml("#1F2937"),
                    AutoSize = true,
                    Location = new Point(0, yPos)
                };
                pnlDetailContent.Controls.Add(lblAssets);
                yPos += 35;

                // DataGridView for assets
                var dgvAssets = new DataGridView
                {
                    Location = new Point(0, yPos),
                    Width = pnlDetailContent.ClientSize.Width - 40,
                    Height = 250,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowHeadersVisible = false,
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersHeight = 35,
                    RowTemplate = { Height = 35 }
                };

                UIHelper.StyleDataGridView(dgvAssets);
                UIHelper.AddColumn(dgvAssets, "TenTaiSan", "Tên tài sản", "TenTaiSan", 200);
                UIHelper.AddColumn(dgvAssets, "SoLuong", "Số lượng", "SoLuong", 80);
                UIHelper.AddColumn(dgvAssets, "TinhTrang", "Tình trạng", "TinhTrang", 120);
                UIHelper.AddColumn(dgvAssets, "GhiChu", "Ghi chú", "GhiChu", 150);

                try
                {
                    var assets = await _taiSanRepo.GetByPhongIdAsync(room.PhongId);
                    dgvAssets.DataSource = assets.ToList();
                }
                catch
                {
                    // Ignore
                }

                pnlDetailContent.Controls.Add(dgvAssets);

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
            this.Name = "ucMyRoom";
            this.Size = new Size(1100, 700);
        }
    }
}
