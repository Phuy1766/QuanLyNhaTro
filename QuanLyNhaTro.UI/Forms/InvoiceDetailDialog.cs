using QuanLyNhaTro.DAL.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class InvoiceDetailDialog : Form
    {
        private readonly HoaDon _invoice;

        public InvoiceDetailDialog(HoaDon invoice)
        {
            _invoice = invoice;

            InitializeComponent();
            BuildModernUI();
        }

        private void BuildModernUI()
        {
            // Form settings
            this.Text = $"Chi tiết hóa đơn – {_invoice.MaHoaDon}";
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

            // Card 1: Invoice Info
            var cardInfo = CreateInvoiceInfoCard();
            cardInfo.Location = new Point(0, yPos);
            cardInfo.Width = 690;
            pnlContent.Controls.Add(cardInfo);
            yPos += cardInfo.Height + 18;

            // Card 2: Charges Breakdown
            var cardCharges = CreateChargesCard();
            cardCharges.Location = new Point(0, yPos);
            cardCharges.Width = 690;
            pnlContent.Controls.Add(cardCharges);
            yPos += cardCharges.Height + 18;

            // Card 3: Payment Summary
            var cardPayment = CreatePaymentSummaryCard();
            cardPayment.Location = new Point(0, yPos);
            cardPayment.Width = 690;
            pnlContent.Controls.Add(cardPayment);
            yPos += cardPayment.Height + 18;

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

        private Panel CreateInvoiceInfoCard()
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
                BackColor = Color.FromArgb(30, 136, 229),
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
                Text = "Thông tin hóa đơn",
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

            CreateInfoRow(card, "Mã hóa đơn:", _invoice.MaHoaDon, xLeft, yPos);
            CreateInfoRow(card, "Phòng:", _invoice.MaPhong ?? "---", xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Tháng:", $"{_invoice.ThangNam:MM/yyyy}", xLeft, yPos);
            CreateInfoRow(card, "Hạn thanh toán:", $"{_invoice.NgayHetHan:dd/MM/yyyy}", xRight, yPos);

            yPos += 35;
            var statusText = GetStatusDisplay(_invoice.TrangThai);
            var statusColor = GetStatusColor(_invoice.TrangThai);

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

        private Panel CreateChargesCard()
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
                BackColor = Color.FromArgb(255, 152, 0),
                Padding = new Padding(20, 12, 20, 12)
            };

            var lblHeaderIcon = new Label
            {
                Text = "📋",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Chi tiết các khoản phí",
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

            CreateInfoRow(card, "Tiền phòng:", $"{_invoice.GiaPhong:N0} VNĐ", xLeft, yPos);
            CreateInfoRow(card, "Tiền điện:", $"{_invoice.TienDien:N0} VNĐ", xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Tiền nước:", $"{_invoice.TienNuoc:N0} VNĐ", xLeft, yPos);
            CreateInfoRow(card, "Dịch vụ khác:", $"{_invoice.TienDichVu:N0} VNĐ", xRight, yPos);

            return card;
        }

        private Panel CreatePaymentSummaryCard()
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = 180,
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
                Text = "💳",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Tổng kết thanh toán",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Location = new Point(60, 14),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblHeaderIcon, lblHeaderTitle });
            card.Controls.Add(pnlHeader);

            var yPos = 70;
            var xLeft = 30;

            CreateInfoRow(card, "Tổng cộng:", $"{_invoice.TongCong:N0} VNĐ", xLeft, yPos, true);

            yPos += 35;
            CreateInfoRow(card, "Đã thanh toán:", $"{_invoice.DaThanhToan:N0} VNĐ", xLeft, yPos);

            yPos += 35;
            var debtColor = _invoice.ConNo > 0 ? Color.FromArgb(231, 76, 60) : Color.FromArgb(39, 174, 96);

            var lblDebtLabel = new Label
            {
                Text = "Còn nợ:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(xLeft, yPos),
                Size = new Size(130, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblDebtValue = new Label
            {
                Text = $"{_invoice.ConNo:N0} VNĐ",
                Font = new Font("Segoe UI Semibold", 12F),
                ForeColor = debtColor,
                Location = new Point(xLeft + 140, yPos),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblDebtLabel, lblDebtValue });

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

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "DaThanhToan" => "Đã thanh toán",
                "ChuaThanhToan" => "Chưa thanh toán",
                "ChoXacNhan" => "Chờ xác nhận",
                "QuaHan" => "Quá hạn",
                _ => status
            };
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "DaThanhToan" => Color.FromArgb(39, 174, 96), // Green
                "ChuaThanhToan" => Color.FromArgb(255, 193, 7), // Yellow
                "ChoXacNhan" => Color.FromArgb(59, 130, 246), // Blue
                "QuaHan" => Color.FromArgb(231, 76, 60), // Red
                _ => Color.FromArgb(149, 165, 166) // Gray
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 550);
            this.Name = "InvoiceDetailDialog";
            this.ResumeLayout(false);
        }
    }
}
