using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class BookingRequestDetailDialog : Form
    {
        private readonly YeuCauThuePhong _request;
        private readonly BookingRequestDTO? _paymentInfo;

        public BookingRequestDetailDialog(YeuCauThuePhong request, BookingRequestDTO? paymentInfo)
        {
            _request = request;
            _paymentInfo = paymentInfo;

            InitializeComponent();
            BuildModernUI();
        }

        private void BuildModernUI()
        {
            // Form settings
            this.Text = $"Chi tiết yêu cầu #{_request.MaYeuCau}";
            this.Size = new Size(750, 600);
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

            // Card 1: Request Info
            var cardRequest = CreateRequestInfoCard();
            cardRequest.Location = new Point(0, yPos);
            cardRequest.Width = 690;
            pnlContent.Controls.Add(cardRequest);
            yPos += cardRequest.Height + 18;

            // Card 2: Room Info
            var cardRoom = CreateRoomInfoCard();
            cardRoom.Location = new Point(0, yPos);
            cardRoom.Width = 690;
            pnlContent.Controls.Add(cardRoom);
            yPos += cardRoom.Height + 18;

            // Card 3: Payment Info (if available)
            if (_paymentInfo != null)
            {
                var cardPayment = CreatePaymentInfoCard();
                cardPayment.Location = new Point(0, yPos);
                cardPayment.Width = 690;
                pnlContent.Controls.Add(cardPayment);
                yPos += cardPayment.Height + 18;
            }

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

        private Panel CreateRequestInfoCard()
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Height = 220,
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
                Text = "📋",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Thông tin yêu cầu",
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

            CreateInfoRow(card, "Mã yêu cầu:", _request.MaYeuCau.ToString(), xLeft, yPos);
            CreateInfoRow(card, "Ngày gửi:", _request.NgayGui.ToString("dd/MM/yyyy HH:mm"), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Phòng:", _request.MaPhong ?? "---", xLeft, yPos);
            CreateInfoRow(card, "Giá thuê:", $"{_request.GiaPhong:N0} VNĐ/tháng", xRight, yPos);

            yPos += 35;
            var statusText = GetStatusDisplay(_request.TrangThai);
            var statusColor = GetStatusColor(_request.TrangThai);

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

            yPos += 35;
            if (!string.IsNullOrEmpty(_request.GhiChu))
            {
                CreateInfoRow(card, "Ghi chú:", _request.GhiChu, xLeft, yPos);
            }

            return card;
        }

        private Panel CreateRoomInfoCard()
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
                Text = "🏠",
                Font = new Font("Segoe UI", 18F),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderTitle = new Label
            {
                Text = "Thông tin phòng đăng ký",
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

            CreateInfoRow(card, "Tòa nhà:", _request.TenToaNha ?? "---", xLeft, yPos);
            CreateInfoRow(card, "Diện tích:", _request.DienTich.HasValue ? $"{_request.DienTich.Value} m²" : "---", xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Số người:", _request.SoNguoiToiDa?.ToString() ?? "---", xLeft, yPos);
            CreateInfoRow(card, "Số người thuê:", _request.SoNguoi.ToString(), xRight, yPos);

            yPos += 35;
            CreateInfoRow(card, "Giá thuê:", $"{_request.GiaPhong:N0} VNĐ/tháng", xLeft, yPos, true);

            return card;
        }

        private Panel CreatePaymentInfoCard()
        {
            if (_paymentInfo == null) return new Panel();

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
                BackColor = Color.FromArgb(255, 152, 0),
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
                Text = "Thông tin thanh toán",
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

            var soTienCoc = _paymentInfo.SoTienCoc.HasValue ? $"{_paymentInfo.SoTienCoc.Value:N0} VNĐ" : "---";
            CreateInfoRow(card, "Số tiền cọc:", soTienCoc, xLeft, yPos);

            var maThanhToan = _paymentInfo.MaThanhToan.HasValue ? _paymentInfo.MaThanhToan.Value.ToString() : "---";
            CreateInfoRow(card, "Mã TT:", maThanhToan, xRight, yPos);

            yPos += 35;
            if (_paymentInfo.NgayThanhToan.HasValue)
            {
                CreateInfoRow(card, "Ngày TT:", _paymentInfo.NgayThanhToan.Value.ToString("dd/MM/yyyy HH:mm"), xLeft, yPos);
            }

            var paymentStatus = _paymentInfo.TrangThaiThanhToan ?? "Chưa có";
            var paymentColor = paymentStatus.Contains("Thành công") || paymentStatus.Contains("Đã thanh toán")
                ? Color.FromArgb(40, 167, 69)
                : Color.FromArgb(255, 193, 7);

            yPos += 35;
            var lblPaymentLabel = new Label
            {
                Text = "Trạng thái TT:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(xLeft, yPos),
                AutoSize = true
            };

            var lblPaymentValue = new Label
            {
                Text = paymentStatus,
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = paymentColor,
                Location = new Point(xLeft + 140, yPos),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[] { lblPaymentLabel, lblPaymentValue });

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
                "PendingPayment" => "Chờ thanh toán",
                "WaitingConfirm" => "Chờ xác nhận",
                "Pending" => "Đang xử lý",
                "Approved" => "Đã duyệt",
                "Rejected" => "Từ chối",
                "Canceled" => "Đã hủy",
                _ => status
            };
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "Approved" => Color.FromArgb(16, 185, 129),
                "Rejected" => Color.FromArgb(239, 68, 68),
                "Canceled" => Color.FromArgb(107, 114, 128),
                "PendingPayment" => Color.FromArgb(255, 193, 7),
                "WaitingConfirm" => Color.FromArgb(59, 130, 246),
                "Pending" => Color.FromArgb(139, 92, 246),
                _ => Color.FromArgb(107, 114, 128)
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 600);
            this.Name = "BookingRequestDetailDialog";
            this.ResumeLayout(false);
        }
    }
}
