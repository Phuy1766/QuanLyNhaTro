using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucTenantNotification : UserControl
    {
        private readonly NotificationRepository _notiRepo = new();
        private readonly int _tenantUserId;

        private Panel pnlNotifications = null!;
        private Label lblCount = null!;

        public ucTenantNotification(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            CreateLayout();
            LoadDataAsync();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;
            this.AutoScroll = true;

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };

            var lblTitle = new Label
            {
                Text = "Thông báo",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblCount = new Label
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = ThemeManager.TextSecondary,
                AutoSize = true,
                Location = new Point(180, 25)
            };
            pnlHeader.Controls.Add(lblCount);

            var btnMarkAll = new Button
            {
                Text = "Đánh dấu tất cả đã đọc",
                Size = new Size(180, 35),
                Location = new Point(870, 17),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnMarkAll.Click += BtnMarkAll_Click;
            pnlHeader.Controls.Add(btnMarkAll);
            this.Controls.Add(pnlHeader);

            // Notifications container
            pnlNotifications = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ThemeManager.Background,
                Padding = new Padding(20)
            };
            this.Controls.Add(pnlNotifications);
        }

        private async void LoadDataAsync()
        {
            try
            {
                var notifications = await _notiRepo.GetAllAsync(_tenantUserId);
                var list = notifications.ToList();

                var unreadCount = list.Count(n => !n.DaDoc);
                lblCount.Text = $"({unreadCount} chưa đọc)";

                pnlNotifications.Controls.Clear();
                int y = 10;

                foreach (var noti in list)
                {
                    var card = CreateNotificationCard(noti, y);
                    pnlNotifications.Controls.Add(card);
                    y += 90;
                }

                if (!list.Any())
                {
                    var lblEmpty = new Label
                    {
                        Text = "Không có thông báo nào",
                        Font = new Font("Segoe UI", 12),
                        ForeColor = ThemeManager.TextSecondary,
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    pnlNotifications.Controls.Add(lblEmpty);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private Panel CreateNotificationCard(Notification noti, int y)
        {
            var card = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(1020, 80),
                BackColor = noti.DaDoc ? ThemeManager.Surface : Color.FromArgb(240, 249, 255),
                Cursor = Cursors.Hand,
                Tag = noti.NotificationId
            };

            // Icon based on type
            string icon = noti.LoaiThongBao switch
            {
                "HoaDon" => "💰",
                "HopDong" => "📝",
                "BaoTri" => "🛠",
                "HeThong" => "⚙",
                _ => "🔔"
            };

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 20),
                Location = new Point(15, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = noti.TieuDe,
                Font = new Font("Segoe UI", 11, noti.DaDoc ? FontStyle.Regular : FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(70, 12),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            var lblContent = new Label
            {
                Text = noti.NoiDung,
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextSecondary,
                Location = new Point(70, 38),
                Size = new Size(800, 20)
            };
            card.Controls.Add(lblContent);

            var lblTime = new Label
            {
                Text = noti.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(70, 58),
                AutoSize = true
            };
            card.Controls.Add(lblTime);

            if (!noti.DaDoc)
            {
                var dot = new Panel
                {
                    Size = new Size(10, 10),
                    Location = new Point(1000, 35),
                    BackColor = Color.FromArgb(59, 130, 246)
                };
                card.Controls.Add(dot);
            }

            card.Click += async (s, e) =>
            {
                if (!noti.DaDoc)
                {
                    await _notiRepo.MarkAsReadAsync(noti.NotificationId);
                    LoadDataAsync();
                }
            };

            foreach (Control c in card.Controls)
            {
                c.Click += async (s, e) =>
                {
                    if (!noti.DaDoc)
                    {
                        await _notiRepo.MarkAsReadAsync(noti.NotificationId);
                        LoadDataAsync();
                    }
                };
            }

            return card;
        }

        private async void BtnMarkAll_Click(object? sender, EventArgs e)
        {
            try
            {
                await _notiRepo.MarkAllAsReadAsync(_tenantUserId);
                LoadDataAsync();
                UIHelper.ShowSuccess("Đã đánh dấu tất cả là đã đọc");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucTenantNotification";
            this.Size = new Size(1100, 700);
        }
    }
}
