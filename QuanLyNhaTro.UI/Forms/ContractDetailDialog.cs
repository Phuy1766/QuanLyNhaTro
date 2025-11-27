using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class ContractDetailDialog : Form
    {
        private readonly HopDong _contract;

        public ContractDetailDialog(HopDong contract)
        {
            _contract = contract;

            InitializeComponent();
            BuildModernUI();
        }

        private void BuildModernUI()
        {
            // Form settings
            this.Text = $"Chi tiết hợp đồng – {_contract.MaHopDong}";
            this.Size = new Size(750, 550);
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

            // Card 1: Contract Info
            var cardContract = CreateContractInfoCard();
            cardContract.Location = new Point(0, yPos);
            cardContract.Width = 690;
            pnlContent.Controls.Add(cardContract);
            yPos += cardContract.Height + 18;

            // Card 2: Financial Info
            var cardFinance = CreateFinanceInfoCard();
            cardFinance.Location = new Point(0, yPos);
            cardFinance.Width = 690;
            pnlContent.Controls.Add(cardFinance);
            yPos += cardFinance.Height + 18;

            pnlContent.Height = yPos;
            pnlMain.Controls.Add(pnlContent);

            // Footer
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

        private Panel CreateContractInfoCard()
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = 240,
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 136, 229),
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

            // Content
            var yPos = 70;
            var xLeft = 30;
            var xRight = 370;

            CreateInfoRow(card, "Mã hợp đồng:", _contract.MaHopDong, xLeft, yPos);
            CreateInfoRow(card, "Phòng:", _contract.MaPhong ?? "---", xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Ngày bắt đầu:", _contract.NgayBatDau.ToString("dd/MM/yyyy"), xLeft, yPos);
            CreateInfoRow(card, "Ngày kết thúc:", _contract.NgayKetThuc.ToString("dd/MM/yyyy"), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Tòa nhà:", _contract.BuildingName ?? "---", xLeft, yPos);
            CreateInfoRow(card, "Chu kỳ TT:", $"{_contract.ChuKyThanhToan} tháng", xRight, yPos);

            yPos += 35;
            var statusText = GetStatusDisplay();
            var statusColor = GetStatusColor();

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
                Text = statusText,
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = statusColor,
                Location = new Point(xLeft + 140, yPos),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblStatusLabel, lblStatusValue });

            return card;
        }

        private Panel CreateFinanceInfoCard()
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = 200,
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(40, 167, 69),
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblHeaderIcon = new Label
            {
                Text = "💰",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Thông tin tài chính",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Location = new Point(60, 14),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblHeaderIcon, lblHeaderTitle });
            card.Controls.Add(pnlHeader);

            var yPos = 70;
            var xLeft = 30;
            var xRight = 370;

            CreateInfoRow(card, "Giá thuê:", $"{_contract.GiaThue:N0} VNĐ/tháng", xLeft, yPos, true);
            CreateInfoRow(card, "Tiền cọc:", $"{_contract.TienCoc:N0} VNĐ", xRight, yPos, true);

            yPos += 35;
            if (!string.IsNullOrEmpty(_contract.GhiChu))
            {
                var lblNoteLabel = new Label
                {
                    Text = "Ghi chú:",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    Location = new Point(xLeft, yPos),
                    AutoSize = true
                };

                var lblNoteValue = new Label
                {
                    Text = _contract.GhiChu,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(33, 37, 41),
                    Location = new Point(xLeft + 140, yPos),
                    MaximumSize = new Size(500, 0),
                    AutoSize = true
                };

                card.Controls.AddRange(new Control[] { lblNoteLabel, lblNoteValue });
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

        private string GetStatusDisplay()
        {
            if (_contract.TrangThai == "Active" && _contract.NgayKetThuc < DateTime.Now)
            {
                return "Đã hết hạn";
            }
            else if (_contract.TrangThai == "Active")
            {
                var daysRemaining = (_contract.NgayKetThuc - DateTime.Now).Days;
                if (daysRemaining <= 30)
                {
                    return $"Hiệu lực (còn {daysRemaining} ngày)";
                }
                return "Đang hiệu lực";
            }
            else if (_contract.TrangThai == "Expired")
            {
                return "Hết hạn";
            }
            return _contract.TrangThai;
        }

        private Color GetStatusColor()
        {
            if (_contract.TrangThai == "Active" && _contract.NgayKetThuc < DateTime.Now)
            {
                return Color.FromArgb(231, 76, 60); // Red - Expired
            }
            else if (_contract.TrangThai == "Active")
            {
                var daysRemaining = (_contract.NgayKetThuc - DateTime.Now).Days;
                if (daysRemaining <= 30)
                {
                    return Color.FromArgb(255, 193, 7); // Yellow - Expiring soon
                }
                return Color.FromArgb(39, 174, 96); // Green - Active
            }
            else if (_contract.TrangThai == "Expired")
            {
                return Color.FromArgb(231, 76, 60); // Red
            }
            return Color.FromArgb(149, 165, 166); // Gray
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 550);
            this.Name = "ContractDetailDialog";
            this.ResumeLayout(false);
        }
    }
}
