using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class RoomDetailDialog : Form
    {
        private readonly PhongTro _room;
        private readonly HopDong _contract;
        private readonly List<TaiSanPhong> _assets;

        public RoomDetailDialog(PhongTro room, HopDong contract, List<TaiSanPhong> assets)
        {
            _room = room;
            _contract = contract;
            _assets = assets ?? new List<TaiSanPhong>();

            InitializeComponent();
            BuildModernUI();
        }

        private void BuildModernUI()
        {
            // Form settings
            this.Text = $"Chi tiết phòng – {_room.MaPhong}";
            this.Size = new Size(750, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(247, 249, 252);
            this.Padding = new Padding(20);

            // Main container
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            var pnlContent = new Panel
            {
                Location = new Point(0, 0),
                Width = 690,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            int yPos = 0;

            // ===== CARD 1: ROOM INFO =====
            var cardRoom = CreateRoomInfoCard();
            cardRoom.Location = new Point(0, yPos);
            cardRoom.Width = 690;
            pnlContent.Controls.Add(cardRoom);
            yPos += cardRoom.Height + 18;

            // ===== CARD 2: CONTRACT INFO =====
            var cardContract = CreateContractInfoCard();
            cardContract.Location = new Point(0, yPos);
            cardContract.Width = 690;
            pnlContent.Controls.Add(cardContract);
            yPos += cardContract.Height + 18;

            // ===== CARD 3: ASSETS =====
            var cardAssets = CreateAssetsCard();
            cardAssets.Location = new Point(0, yPos);
            cardAssets.Width = 690;
            pnlContent.Controls.Add(cardAssets);
            yPos += cardAssets.Height + 18;

            pnlContent.Height = yPos;
            pnlMain.Controls.Add(pnlContent);

            // ===== FOOTER BUTTONS =====
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };

            var btnClose = new Button
            {
                Text = "Đóng",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 136, 229),
                Size = new Size(120, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right
            };
            btnClose.Location = new Point(pnlFooter.Width - 140, 12);
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.FromArgb(25, 118, 210);
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.FromArgb(30, 136, 229);

            pnlFooter.Controls.Add(btnClose);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlFooter);
        }

        private Panel CreateRoomInfoCard()
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = 220,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Card header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 136, 229),
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblHeaderIcon = new Label
            {
                Text = "🏠",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Thông tin phòng",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Location = new Point(60, 14),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblHeaderIcon, lblHeaderTitle });
            card.Controls.Add(pnlHeader);

            // Content
            var yPos = 70;
            var xLeft = 30;
            var xRight = 370;

            CreateInfoRow(card, "Mã phòng:", _room.MaPhong, xLeft, yPos);
            CreateInfoRow(card, "Tòa nhà:", _room.BuildingName ?? "---", xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Loại phòng:", _room.TenLoai ?? "---", xLeft, yPos);
            CreateInfoRow(card, "Tầng:", _room.Tang.ToString(), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Diện tích:", $"{_room.DienTich} m²", xLeft, yPos);
            CreateInfoRow(card, "Số người tối đa:", _room.SoNguoiToiDa.ToString(), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Giá thuê:", $"{_room.GiaThue:N0} VNĐ/tháng", xLeft, yPos, true);

            return card;
        }

        private Panel CreateContractInfoCard()
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = 220,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Card header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(40, 167, 69),
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblHeaderIcon = new Label
            {
                Text = "📄",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Thông tin hợp đồng",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Location = new Point(60, 14),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblHeaderIcon, lblHeaderTitle });
            card.Controls.Add(pnlHeader);

            // Contract status
            var contractStatusDisplay = "Đang hiệu lực";
            var statusColor = Color.FromArgb(39, 174, 96);
            if (_contract.TrangThai == "Active" && _contract.NgayKetThuc < DateTime.Now)
            {
                contractStatusDisplay = "Đã hết hạn";
                statusColor = Color.FromArgb(231, 76, 60);
            }

            // Content
            var yPos = 70;
            var xLeft = 30;
            var xRight = 370;

            CreateInfoRow(card, "Mã hợp đồng:", _contract.MaHopDong, xLeft, yPos);
            CreateInfoRow(card, "Ngày bắt đầu:", _contract.NgayBatDau.ToString("dd/MM/yyyy"), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Giá thuê:", $"{_contract.GiaThue:N0} VNĐ/tháng", xLeft, yPos);
            CreateInfoRow(card, "Ngày kết thúc:", _contract.NgayKetThuc.ToString("dd/MM/yyyy"), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Tiền cọc:", $"{_contract.TienCoc:N0} VNĐ", xLeft, yPos);
            CreateInfoRow(card, "Chu kỳ TT:", $"{_contract.ChuKyThanhToan} tháng", xRight, yPos);

            yPos += 35;
            var lblStatusLabel = new Label
            {
                Text = "Trạng thái:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(xLeft, yPos),
                AutoSize = true
            };

            var lblStatusValue = new Label
            {
                Text = contractStatusDisplay,
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = statusColor,
                Location = new Point(xLeft + 140, yPos),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblStatusLabel, lblStatusValue });

            return card;
        }

        private Panel CreateAssetsCard()
        {
            var cardHeight = _assets.Count == 0 ? 120 : Math.Min(350, 100 + (_assets.Count * 35));

            var card = new Panel
            {
                BackColor = Color.White,
                Height = cardHeight,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Card header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(255, 152, 0),
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblHeaderIcon = new Label
            {
                Text = "📦",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = $"Tài sản trong phòng ({_assets.Count})",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Location = new Point(60, 14),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblHeaderIcon, lblHeaderTitle });
            card.Controls.Add(pnlHeader);

            if (_assets.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "Không có tài sản nào trong phòng này",
                    Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(149, 165, 166),
                    Location = new Point(30, 75),
                    AutoSize = true
                };
                card.Controls.Add(lblEmpty);
            }
            else
            {
                // Create assets table
                var dgvAssets = new DataGridView
                {
                    Location = new Point(20, 65),
                    Size = new Size(650, cardHeight - 80),
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    ReadOnly = true,
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersHeight = 38,
                    RowTemplate = { Height = 32 },
                    ScrollBars = ScrollBars.Vertical,
                    ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(247, 249, 252),
                        ForeColor = Color.FromArgb(73, 80, 87),
                        Font = new Font("Segoe UI Semibold", 10F),
                        Alignment = DataGridViewContentAlignment.MiddleLeft,
                        Padding = new Padding(10, 0, 10, 0)
                    },
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Padding = new Padding(10, 4, 10, 4),
                        Font = new Font("Segoe UI", 10F),
                        SelectionBackColor = Color.FromArgb(225, 242, 255),
                        SelectionForeColor = Color.FromArgb(33, 37, 41),
                        Alignment = DataGridViewContentAlignment.MiddleLeft
                    },
                    AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(252, 253, 254)
                    }
                };

                // Columns
                dgvAssets.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TenTaiSan",
                    HeaderText = "Tài sản",
                    DataPropertyName = "TenTaiSan",
                    Width = 350,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                dgvAssets.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "SoLuong",
                    HeaderText = "SL",
                    DataPropertyName = "SoLuong",
                    Width = 80,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvAssets.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TinhTrang",
                    HeaderText = "Tình trạng",
                    DataPropertyName = "TinhTrang",
                    Width = 150
                });

                dgvAssets.DataSource = _assets;

                // Color coding for TinhTrang
                dgvAssets.CellFormatting += (s, e) =>
                {
                    if (e.RowIndex < 0) return;

                    if (dgvAssets.Columns[e.ColumnIndex].Name == "TinhTrang")
                    {
                        var value = e.Value?.ToString() ?? "";
                        var cell = dgvAssets.Rows[e.RowIndex].Cells[e.ColumnIndex];

                        if (value.Contains("Tốt"))
                        {
                            cell.Style.ForeColor = Color.FromArgb(39, 174, 96);
                            cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                        }
                        else if (value.Contains("Hư") || value.Contains("Hỏng"))
                        {
                            cell.Style.ForeColor = Color.FromArgb(231, 76, 60);
                            cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                        }
                        else
                        {
                            cell.Style.ForeColor = Color.FromArgb(255, 152, 0);
                            cell.Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                        }
                    }
                };

                card.Controls.Add(dgvAssets);
            }

            return card;
        }

        private void CreateInfoRow(Panel parent, string label, string value, int x, int y, bool bold = false)
        {
            var lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(x, y),
                Size = new Size(130, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblValue = new Label
            {
                Text = value,
                Font = bold ? new Font("Segoe UI Semibold", 11F) : new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(x + 140, y),
                AutoSize = true,
                MaximumSize = new Size(180, 0)
            };

            parent.Controls.AddRange(new Control[] { lblLabel, lblValue });
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 650);
            this.Name = "RoomDetailDialog";
            this.ResumeLayout(false);
        }
    }
}
