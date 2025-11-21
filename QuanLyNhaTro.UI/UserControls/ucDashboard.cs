using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucDashboard : UserControl
    {
        private readonly DashboardRepository _repo = new();

        // Stat Cards
        private Panel pnlCards = null!;
        private Label lblTotalRooms = null!;
        private Label lblAvailableRooms = null!;
        private Label lblTenants = null!;
        private Label lblRevenue = null!;
        private Label lblDebt = null!;
        private Label lblExpiring = null!;

        // Lists
        private DataGridView dgvExpiring = null!;
        private DataGridView dgvOverdue = null!;

        // Charts
        private Panel pnlRevenueChart = null!;
        private Panel pnlRoomChart = null!;

        public ucDashboard()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;
            this.Padding = new Padding(0);

            // Main scroll container
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            // ===== STAT CARDS ROW =====
            pnlCards = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1140, 130),
                BackColor = Color.Transparent
            };

            var cardData = new[]
            {
                ("Tổng số phòng", "0", ThemeManager.CardBlue, "🏠"),
                ("Phòng trống", "0", ThemeManager.CardGreen, "✅"),
                ("Khách thuê", "0", ThemeManager.CardPurple, "👥"),
                ("Doanh thu tháng", "0 ₫", ThemeManager.CardPink, "💰"),
                ("Tổng công nợ", "0 ₫", ThemeManager.CardRed, "⚠️"),
                ("HĐ sắp hết hạn", "0", ThemeManager.CardOrange, "📅"),
            };

            int cardWidth = 180;
            int spacing = 12;
            int x = 0;

            for (int i = 0; i < cardData.Length; i++)
            {
                var card = CreateModernStatCard(cardData[i].Item1, cardData[i].Item2, cardData[i].Item3, cardData[i].Item4, x);
                pnlCards.Controls.Add(card);

                var lblValue = card.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "value");
                if (lblValue != null)
                {
                    switch (i)
                    {
                        case 0: lblTotalRooms = lblValue; break;
                        case 1: lblAvailableRooms = lblValue; break;
                        case 2: lblTenants = lblValue; break;
                        case 3: lblRevenue = lblValue; break;
                        case 4: lblDebt = lblValue; break;
                        case 5: lblExpiring = lblValue; break;
                    }
                }
                x += cardWidth + spacing;
            }

            // ===== CHARTS ROW =====
            var pnlChartsRow = new Panel
            {
                Location = new Point(0, 145),
                Size = new Size(1140, 220),
                BackColor = Color.Transparent
            };

            // Revenue Chart Card
            var pnlRevenueCard = CreateCardPanel(0, 0, 740, 220, "📊 Doanh thu 6 tháng gần đây");
            pnlRevenueChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(10)
            };
            pnlRevenueChart.Paint += PnlRevenueChart_Paint;
            pnlRevenueCard.Controls.Add(pnlRevenueChart);
            pnlChartsRow.Controls.Add(pnlRevenueCard);

            // Room Status Pie Chart
            var pnlPieCard = CreateCardPanel(755, 0, 385, 220, "🏠 Tình trạng phòng");
            pnlRoomChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(10)
            };
            pnlRoomChart.Paint += PnlRoomChart_Paint;
            pnlPieCard.Controls.Add(pnlRoomChart);
            pnlChartsRow.Controls.Add(pnlPieCard);

            // ===== DATA TABLES ROW =====
            var pnlTablesRow = new Panel
            {
                Location = new Point(0, 380),
                Size = new Size(1140, 320),
                BackColor = Color.Transparent
            };

            // Expiring Contracts Card
            var pnlExpiringCard = CreateCardPanel(0, 0, 560, 320, "📅 Hợp đồng sắp hết hạn (30 ngày)");
            dgvExpiring = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Surface,
                BorderStyle = BorderStyle.None
            };
            StyleModernDataGridView(dgvExpiring);
            SetupExpiringColumns();
            pnlExpiringCard.Controls.Add(dgvExpiring);
            pnlTablesRow.Controls.Add(pnlExpiringCard);

            // Overdue Invoices Card
            var pnlOverdueCard = CreateCardPanel(575, 0, 565, 320, "⚠️ Hóa đơn quá hạn");
            dgvOverdue = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Surface,
                BorderStyle = BorderStyle.None
            };
            StyleModernDataGridView(dgvOverdue);
            SetupOverdueColumns();
            pnlOverdueCard.Controls.Add(dgvOverdue);
            pnlTablesRow.Controls.Add(pnlOverdueCard);

            scrollPanel.Controls.Add(pnlTablesRow);
            scrollPanel.Controls.Add(pnlChartsRow);
            scrollPanel.Controls.Add(pnlCards);

            this.Controls.Add(scrollPanel);
        }

        private Panel CreateModernStatCard(string title, string value, Color accentColor, string icon, int x)
        {
            var card = new Panel
            {
                Location = new Point(x, 0),
                Size = new Size(180, 110),
                BackColor = ThemeManager.Surface
            };
            UIHelper.RoundControl(card, 16);

            // Left accent bar
            var accent = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(5, 110),
                BackColor = accentColor
            };

            // Icon with background
            var pnlIcon = new Panel
            {
                Location = new Point(15, 15),
                Size = new Size(44, 44),
                BackColor = Color.FromArgb(30, accentColor)
            };
            UIHelper.RoundControl(pnlIcon, 12);

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 18),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            pnlIcon.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(15, 65),
                Size = new Size(150, 18),
                BackColor = Color.Transparent
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI Semibold", 18),
                ForeColor = accentColor,
                Location = new Point(70, 18),
                Size = new Size(100, 35),
                Tag = "value",
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.AddRange(new Control[] { accent, pnlIcon, lblTitle, lblValue });
            return card;
        }

        private Panel CreateCardPanel(int x, int y, int width, int height, string title)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };
            UIHelper.RoundControl(card, 16);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = ThemeManager.TextPrimary,
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblTitle);
            return card;
        }

        private void StyleModernDataGridView(DataGridView dgv)
        {
            UIHelper.StyleDataGridView(dgv);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Primary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersHeight = 42;
            dgv.RowTemplate.Height = 38;
            dgv.GridColor = ThemeManager.Border;
            dgv.DefaultCellStyle.BackColor = ThemeManager.Surface;
            dgv.DefaultCellStyle.ForeColor = ThemeManager.TextPrimary;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = ThemeManager.SurfaceHover;
        }

        private void SetupExpiringColumns()
        {
            dgvExpiring.Columns.Clear();
            UIHelper.AddColumn(dgvExpiring, "MaHopDong", "Mã HĐ", "MaHopDong", 90);
            UIHelper.AddColumn(dgvExpiring, "MaPhong", "Phòng", "MaPhong", 70);
            UIHelper.AddColumn(dgvExpiring, "TenKhachThue", "Khách thuê", "TenKhachThue", 130);
            UIHelper.AddColumn(dgvExpiring, "NgayKetThuc", "Ngày hết hạn", "NgayKetThuc", 100);
            UIHelper.AddColumn(dgvExpiring, "SoNgayConLai", "Còn lại", "SoNgayConLai", 70);
        }

        private void SetupOverdueColumns()
        {
            dgvOverdue.Columns.Clear();
            UIHelper.AddColumn(dgvOverdue, "MaHoaDon", "Mã HĐ", "MaHoaDon", 90);
            UIHelper.AddColumn(dgvOverdue, "MaPhong", "Phòng", "MaPhong", 70);
            UIHelper.AddColumn(dgvOverdue, "TenKhachThue", "Khách thuê", "TenKhachThue", 120);
            UIHelper.AddColumn(dgvOverdue, "ConNo", "Còn nợ", "ConNo", 100);
            UIHelper.AddColumn(dgvOverdue, "NgayHetHan", "Hạn TT", "NgayHetHan", 90);
        }

        // Revenue data for chart
        private decimal[] _revenueData = new decimal[6];
        private string[] _monthLabels = new string[6];

        // Room status data for pie chart
        private int _roomsOccupied = 0;
        private int _roomsAvailable = 0;
        private int _roomsMaintenance = 0;

        private void PnlRevenueChart_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = pnlRevenueChart.ClientRectangle;
            int padding = 40;
            int chartWidth = rect.Width - padding * 2;
            int chartHeight = rect.Height - padding * 2;

            if (_revenueData.All(x => x == 0)) return;

            decimal maxVal = _revenueData.Max();
            if (maxVal == 0) maxVal = 1;

            int barWidth = chartWidth / 6 - 15;
            int x = padding;

            for (int i = 0; i < 6; i++)
            {
                int barHeight = (int)((_revenueData[i] / maxVal) * (chartHeight - 20));
                int barY = rect.Height - padding - barHeight;

                // Draw bar with gradient
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(x, barY, barWidth, barHeight),
                    ThemeManager.Primary,
                    ThemeManager.PrimaryLight,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical);

                g.FillRoundedRectangle(brush, x, barY, barWidth, barHeight, 8);

                // Draw month label
                using var font = new Font("Segoe UI", 8);
                using var textBrush = new SolidBrush(ThemeManager.TextSecondary);
                var labelSize = g.MeasureString(_monthLabels[i], font);
                g.DrawString(_monthLabels[i], font, textBrush, x + (barWidth - labelSize.Width) / 2, rect.Height - padding + 5);

                // Draw value
                var valText = (_revenueData[i] / 1000000).ToString("N1") + "M";
                var valSize = g.MeasureString(valText, font);
                g.DrawString(valText, font, textBrush, x + (barWidth - valSize.Width) / 2, barY - 18);

                x += chartWidth / 6;
            }
        }

        private void PnlRoomChart_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int total = _roomsOccupied + _roomsAvailable + _roomsMaintenance;
            if (total == 0) return;

            var rect = pnlRoomChart.ClientRectangle;
            int size = Math.Min(rect.Width - 150, rect.Height - 20);
            int x = 20;
            int y = (rect.Height - size) / 2;

            // Draw pie
            float startAngle = -90;
            var data = new[] {
                (_roomsOccupied, ThemeManager.CardBlue, "Đang thuê"),
                (_roomsAvailable, ThemeManager.CardGreen, "Trống"),
                (_roomsMaintenance, ThemeManager.CardOrange, "Bảo trì")
            };

            foreach (var (value, color, label) in data)
            {
                if (value == 0) continue;
                float sweepAngle = (float)value / total * 360;
                using var brush = new SolidBrush(color);
                g.FillPie(brush, x, y, size, size, startAngle, sweepAngle);
                startAngle += sweepAngle;
            }

            // Draw legend
            int legendX = x + size + 20;
            int legendY = y + 10;

            foreach (var (value, color, label) in data)
            {
                using var brush = new SolidBrush(color);
                g.FillRectangle(brush, legendX, legendY, 14, 14);

                using var font = new Font("Segoe UI", 9);
                using var textBrush = new SolidBrush(ThemeManager.TextPrimary);
                g.DrawString($"{label}: {value}", font, textBrush, legendX + 20, legendY - 2);
                legendY += 28;
            }
        }

        private async void LoadData()
        {
            try
            {
                var stats = await _repo.GetStatsAsync();

                // Update cards
                lblTotalRooms.Text = stats.TongPhong.ToString();
                lblAvailableRooms.Text = stats.PhongTrong.ToString();
                lblTenants.Text = stats.TongKhachThue.ToString();
                lblRevenue.Text = (stats.DoanhThuThang / 1000000).ToString("N1") + "M";
                lblDebt.Text = (stats.TongCongNo / 1000000).ToString("N1") + "M";
                lblExpiring.Text = stats.HopDongSapHetHan.ToString();

                // Room chart data
                _roomsOccupied = stats.TongPhong - stats.PhongTrong;
                _roomsAvailable = stats.PhongTrong;
                _roomsMaintenance = 0;

                // Revenue chart data (mock 6 months)
                var now = DateTime.Now;
                for (int i = 5; i >= 0; i--)
                {
                    var month = now.AddMonths(-i);
                    _monthLabels[5 - i] = month.ToString("MM/yy");
                    _revenueData[5 - i] = stats.DoanhThuThang * (decimal)(0.7 + new Random().NextDouble() * 0.6);
                }
                _revenueData[5] = stats.DoanhThuThang;

                pnlRevenueChart.Invalidate();
                pnlRoomChart.Invalidate();

                // Load expiring contracts
                var hopDongRepo = new HopDongRepository();
                var expiring = await hopDongRepo.GetExpiringSoonAsync(30);
                dgvExpiring.DataSource = expiring.ToList();

                if (dgvExpiring.Columns.Contains("NgayKetThuc"))
                    dgvExpiring.Columns["NgayKetThuc"]!.DefaultCellStyle.Format = "dd/MM/yyyy";

                // Load overdue invoices
                var hoaDonRepo = new HoaDonRepository();
                var overdue = await hoaDonRepo.GetOverdueAsync();
                dgvOverdue.DataSource = overdue.ToList();

                if (dgvOverdue.Columns.Contains("ConNo"))
                    dgvOverdue.Columns["ConNo"]!.DefaultCellStyle.Format = "N0";
                if (dgvOverdue.Columns.Contains("NgayHetHan"))
                    dgvOverdue.Columns["NgayHetHan"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucDashboard";
            this.Size = new Size(1150, 720);
            this.ResumeLayout(false);
        }
    }

    // Extension method for rounded rectangles
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
