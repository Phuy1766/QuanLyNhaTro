using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System.Drawing;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyRoom : UserControl
    {
        private readonly PhongTroRepository _phongRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly TaiSanRepository _taiSanRepo = new();
        private readonly int _tenantUserId;

        private ModernDataGrid dgvRooms = null!;
        private Label lblEmptyMessage = null!;
        private Panel pnlMainCard = null!;

        public ucMyRoom(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            BuildModernUI();
            LoadDataAsync();
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
                Text = "🏠",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 136, 229),
                Location = new Point(0, 8),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Phòng của tôi",
                Font = new Font("Segoe UI Semibold", 24F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(45, 6),
                AutoSize = true
            };

            pnlTitleSection.Controls.AddRange(new Control[] { lblIcon, lblTitle });

            // ===== INFO SUMMARY CARDS - RESPONSIVE GRID =====
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

            // Thiết lập các cột co giãn đều - QUAN TRỌNG: SizeType.Percent
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Card 1: Số phòng đang thuê
            var card1 = CreateInfoCard("📊", "Phòng đang thuê", "0", Color.FromArgb(30, 136, 229));
            card1.Tag = "room_count";
            card1.Margin = new Padding(0, 0, 8, 0);
            card1.Dock = DockStyle.Fill;

            // Card 2: Tòa nhà
            var card2 = CreateInfoCard("🏢", "Tòa nhà", "---", Color.FromArgb(40, 167, 69));
            card2.Tag = "building_name";
            card2.Margin = new Padding(4, 0, 4, 0);
            card2.Dock = DockStyle.Fill;

            // Card 3: Giá thuê
            var card3 = CreateInfoCard("💰", "Giá thuê", "---", Color.FromArgb(255, 152, 0));
            card3.Tag = "gia_thue";
            card3.Margin = new Padding(4, 0, 4, 0);
            card3.Dock = DockStyle.Fill;

            // Card 4: Trạng thái
            var card4 = CreateInfoCard("✓", "Trạng thái HĐ", "---", Color.FromArgb(156, 39, 176));
            card4.Tag = "trang_thai";
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
                Text = "Danh sách phòng đang thuê",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTableTitle.Controls.Add(lblTableTitle);

            // ===== BẢNG DỮ LIỆU - TÁCH RIÊNG, KHÔNG TRONG CARD =====
            pnlMainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle
            };

            // DataGrid
            dgvRooms = new ModernDataGrid
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

            // Hover row
            dgvRooms.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvRooms.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(225, 242, 255);
            };
            dgvRooms.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvRooms.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 249, 250);
            };

            // Cột
            UIHelper.AddColumn(dgvRooms, "MaPhong", "Mã phòng", "MaPhong", 120);
            UIHelper.AddColumn(dgvRooms, "BuildingName", "Tòa nhà", "BuildingName", 180);
            UIHelper.AddColumn(dgvRooms, "NgayBatDau", "Từ ngày", "NgayBatDau", 130);
            UIHelper.AddColumn(dgvRooms, "NgayKetThuc", "Đến ngày", "NgayKetThuc", 130);
            UIHelper.AddColumn(dgvRooms, "GiaThue", "Giá thuê (VNĐ)", "GiaThue", 150);
            UIHelper.AddColumn(dgvRooms, "TrangThaiHopDong", "Trạng thái", "TrangThaiHopDong", 150);

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
            dgvRooms.Columns.Add(btnDetail);

            dgvRooms.CellClick += DgvRooms_CellClick;
            dgvRooms.CellFormatting += DgvRooms_CellFormatting;

            // Empty message
            lblEmptyMessage = new Label
            {
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(149, 165, 166),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };

            // Layout
            pnlMainCard.Controls.AddRange(new Control[] { dgvRooms, lblEmptyMessage });

            // Thêm các controls theo thứ tự dock (bottom to top)
            pnlContainer.Controls.Add(pnlMainCard);        // Dock.Fill - chiếm phần còn lại
            pnlContainer.Controls.Add(pnlTableTitle);      // Dock.Top
            pnlContainer.Controls.Add(pnlCardsContainer);  // Dock.Top
            pnlContainer.Controls.Add(pnlTitleSection);    // Dock.Top

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

            // Icon
            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 28F),
                ForeColor = accentColor,
                Location = new Point(16, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Title
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(75, 22),
                AutoSize = true
            };

            // Value
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

        private void UpdateInfoCards(int roomCount, string buildingName, decimal giaThue, string trangThai)
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
                                case "room_count":
                                    valueLabel.Text = roomCount.ToString();
                                    break;
                                case "building_name":
                                    valueLabel.Text = buildingName.Length > 15 ? buildingName.Substring(0, 15) + "..." : buildingName;
                                    break;
                                case "gia_thue":
                                    valueLabel.Text = $"{giaThue:N0}đ";
                                    valueLabel.Font = new Font("Segoe UI Semibold", 14F);
                                    break;
                                case "trang_thai":
                                    valueLabel.Text = trangThai;
                                    valueLabel.Font = new Font("Segoe UI Semibold", 13F);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                var contracts = (await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId))
                    .Where(c => c.TrangThai == "Active")
                    .OrderBy(c => c.MaPhong)
                    .ToList();

                if (contracts.Count == 0)
                {
                    ShowEmpty("Bạn chưa có phòng nào đang thuê.\n\nHãy đăng ký thuê phòng tại menu 'Tìm phòng trống'.");
                    UpdateInfoCards(0, "---", 0, "---");
                    return;
                }

                HideEmpty();
                dgvRooms.DataSource = contracts;

                // Cập nhật Info Cards
                var firstContract = contracts.First();
                var trangThaiDisplay = firstContract.NgayKetThuc < DateTime.Now ? "Hết hạn" : "Hiệu lực";
                UpdateInfoCards(contracts.Count, firstContract.BuildingName ?? "---", firstContract.GiaThue, trangThaiDisplay);

                // Format
                foreach (DataGridViewColumn col in dgvRooms.Columns)
                {
                    if (col.Name == "GiaThue")
                        col.DefaultCellStyle.Format = "N0";
                    if (col.Name is "NgayBatDau" or "NgayKetThuc")
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
            dgvRooms.Visible = false;
        }

        private void HideEmpty()
        {
            lblEmptyMessage.Visible = false;
            dgvRooms.Visible = true;
            dgvRooms.BringToFront();
        }

        private void DgvRooms_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var contract = dgvRooms.Rows[e.RowIndex].DataBoundItem as HopDong;
            if (contract == null) return;

            var columnName = dgvRooms.Columns[e.ColumnIndex].Name;

            if (columnName == "btnDetail")
            {
                ShowRoomDetail(contract);
            }
        }

        private void DgvRooms_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var contract = dgvRooms.Rows[e.RowIndex].DataBoundItem as HopDong;
            if (contract == null) return;

            // Tùy chỉnh hiển thị trạng thái hợp đồng
            if (dgvRooms.Columns[e.ColumnIndex].Name == "TrangThaiHopDong")
            {
                var cell = dgvRooms.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

                // Kiểm tra hết hạn
                if (contract.TrangThai == "Active" && contract.NgayKetThuc < DateTime.Now)
                {
                    e.Value = "Đã hết hạn";
                    cell.Style.ForeColor = Color.FromArgb(231, 76, 60); // Red
                }
                else if (contract.TrangThai == "Active")
                {
                    e.Value = "Đang hiệu lực";
                    cell.Style.ForeColor = Color.FromArgb(39, 174, 96); // Green
                }
                else
                {
                    e.Value = contract.TrangThai;
                    cell.Style.ForeColor = Color.FromArgb(149, 165, 166); // Gray
                }
            }
        }

        private async void ShowRoomDetail(HopDong contract)
        {
            try
            {
                var room = await _phongRepo.GetByMaPhongAsync(contract.MaPhong);
                if (room == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin phòng", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var assets = (await _taiSanRepo.GetByPhongIdAsync(room.PhongId)).ToList();

                // Show modern dialog
                var dialog = new Forms.RoomDetailDialog(room, contract, assets);
                dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyRoom";
            this.Size = new Size(1100, 700);
        }
    }
}
