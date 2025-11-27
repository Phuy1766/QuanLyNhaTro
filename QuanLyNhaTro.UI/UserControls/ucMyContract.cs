using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyContract : UserControl
    {
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _tenantUserId;

        private ModernDataGrid dgvContracts = null!;
        private Label lblEmptyMessage = null!;
        private Panel pnlMainCard = null!;

        public ucMyContract(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            BuildModernUI();
            LoadContractsAsync();
        }

        private void BuildModernUI()
        {
            this.BackColor = Color.FromArgb(247, 249, 252);
            this.Padding = new Padding(24);

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
                Text = "📄",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 136, 229),
                Location = new Point(0, 8),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "Hợp đồng của tôi",
                Font = new Font("Segoe UI Semibold", 24F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(45, 6),
                AutoSize = true
            };

            pnlTitleSection.Controls.AddRange(new Control[] { lblIcon, lblTitle });

            // ===== INFO SUMMARY CARDS =====
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

            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlCardsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var card1 = CreateInfoCard("📊", "Tổng hợp đồng", "0", Color.FromArgb(30, 136, 229));
            card1.Tag = "total";
            card1.Margin = new Padding(0, 0, 8, 0);
            card1.Dock = DockStyle.Fill;

            var card2 = CreateInfoCard("✓", "Đang hiệu lực", "0", Color.FromArgb(40, 167, 69));
            card2.Tag = "active";
            card2.Margin = new Padding(4, 0, 4, 0);
            card2.Dock = DockStyle.Fill;

            var card3 = CreateInfoCard("⏳", "Sắp hết hạn", "0", Color.FromArgb(255, 193, 7));
            card3.Tag = "expiring";
            card3.Margin = new Padding(4, 0, 4, 0);
            card3.Dock = DockStyle.Fill;

            var card4 = CreateInfoCard("✗", "Đã hết hạn", "0", Color.FromArgb(220, 53, 69));
            card4.Tag = "expired";
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
                Text = "Lịch sử hợp đồng thuê phòng",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTableTitle.Controls.Add(lblTableTitle);

            // ===== BẢNG DỮ LIỆU =====
            pnlMainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle
            };

            dgvContracts = new ModernDataGrid
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

            // Hover effect
            dgvContracts.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvContracts.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(225, 242, 255);
            };
            dgvContracts.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgvContracts.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 249, 250);
            };

            // Columns
            UIHelper.AddColumn(dgvContracts, "MaHopDong", "Mã HĐ", "MaHopDong", 120);
            UIHelper.AddColumn(dgvContracts, "MaPhong", "Phòng", "MaPhong", 100);
            UIHelper.AddColumn(dgvContracts, "NgayBatDau", "Bắt đầu", "NgayBatDau", 120);
            UIHelper.AddColumn(dgvContracts, "NgayKetThuc", "Kết thúc", "NgayKetThuc", 120);
            UIHelper.AddColumn(dgvContracts, "GiaThue", "Giá thuê (VNĐ)", "GiaThue", 140);
            UIHelper.AddColumn(dgvContracts, "TrangThai", "Trạng thái", "TrangThai", 140);

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
            dgvContracts.Columns.Add(btnDetail);

            dgvContracts.CellClick += DgvContracts_CellClick;
            dgvContracts.CellFormatting += DgvContracts_CellFormatting;

            // Empty message
            lblEmptyMessage = new Label
            {
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(149, 165, 166),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlMainCard.Controls.AddRange(new Control[] { dgvContracts, lblEmptyMessage });

            // Layout
            pnlContainer.Controls.Add(pnlMainCard);
            pnlContainer.Controls.Add(pnlTableTitle);
            pnlContainer.Controls.Add(pnlCardsContainer);
            pnlContainer.Controls.Add(pnlTitleSection);

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

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 28F),
                ForeColor = accentColor,
                Location = new Point(16, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(75, 22),
                AutoSize = true
            };

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

        private void UpdateInfoCards(int total, int active, int expiring, int expired)
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
                                case "total":
                                    valueLabel.Text = total.ToString();
                                    break;
                                case "active":
                                    valueLabel.Text = active.ToString();
                                    break;
                                case "expiring":
                                    valueLabel.Text = expiring.ToString();
                                    break;
                                case "expired":
                                    valueLabel.Text = expired.ToString();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async void LoadContractsAsync()
        {
            try
            {
                var contracts = (await _hopDongRepo.GetByTenantUserIdAsync(_tenantUserId))
                    .OrderByDescending(c => c.NgayBatDau)
                    .ToList();

                if (contracts.Count == 0)
                {
                    ShowEmpty("Bạn chưa có hợp đồng thuê phòng nào.\n\nHợp đồng sẽ được tạo sau khi yêu cầu thuê được duyệt.");
                    UpdateInfoCards(0, 0, 0, 0);
                    return;
                }

                HideEmpty();
                dgvContracts.DataSource = contracts;

                // Statistics
                var total = contracts.Count;
                var active = contracts.Count(c => c.TrangThai == "Active" && c.NgayKetThuc >= DateTime.Now);
                var expiring = contracts.Count(c => c.TrangThai == "Active" &&
                    c.NgayKetThuc >= DateTime.Now &&
                    c.NgayKetThuc <= DateTime.Now.AddDays(30));
                var expired = contracts.Count(c => c.TrangThai == "Active" && c.NgayKetThuc < DateTime.Now);

                UpdateInfoCards(total, active, expiring, expired);

                // Format columns
                foreach (DataGridViewColumn col in dgvContracts.Columns)
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
            dgvContracts.Visible = false;
        }

        private void HideEmpty()
        {
            lblEmptyMessage.Visible = false;
            dgvContracts.Visible = true;
            dgvContracts.BringToFront();
        }

        private void DgvContracts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var contract = dgvContracts.Rows[e.RowIndex].DataBoundItem as HopDong;
            if (contract == null) return;

            var columnName = dgvContracts.Columns[e.ColumnIndex].Name;

            if (columnName == "btnDetail")
            {
                ShowContractDetail(contract);
            }
        }

        private void DgvContracts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var contract = dgvContracts.Rows[e.RowIndex].DataBoundItem as HopDong;
            if (contract == null) return;

            if (dgvContracts.Columns[e.ColumnIndex].Name == "TrangThai")
            {
                var cell = dgvContracts.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

                if (contract.TrangThai == "Active" && contract.NgayKetThuc < DateTime.Now)
                {
                    e.Value = "Đã hết hạn";
                    cell.Style.ForeColor = Color.FromArgb(231, 76, 60);
                }
                else if (contract.TrangThai == "Active")
                {
                    e.Value = "Đang hiệu lực";
                    cell.Style.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else if (contract.TrangThai == "Expired")
                {
                    e.Value = "Hết hạn";
                    cell.Style.ForeColor = Color.FromArgb(231, 76, 60);
                }
                else
                {
                    e.Value = contract.TrangThai;
                    cell.Style.ForeColor = Color.FromArgb(149, 165, 166);
                }
            }
        }

        private void ShowContractDetail(HopDong contract)
        {
            try
            {
                var dialog = new Forms.ContractDetailDialog(contract);
                dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyContract";
            this.Size = new Size(1100, 700);
        }
    }
}
