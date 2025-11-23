using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;
using QuanLyNhaTro.UI.UserControls;

namespace QuanLyNhaTro.UI.Forms
{
    public partial class frmMain : Form
    {
        private readonly AuthService _authService = new();
        private readonly NotificationRepository _notiRepo = new();
        private readonly BackgroundTaskService _backgroundService = new();
        private System.Windows.Forms.Timer? _dailyTaskTimer;

        // Controls
        private Panel pnlSidebar = null!;
        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Panel pnlMenuContainer = null!;
        private Label lblTitle = null!;
        private Label lblUserInfo = null!;
        private Label lblNotificationBadge = null!;
        private Button btnThemeToggle = null!;
        private PictureBox picAvatar = null!;
        private List<Panel> menuItems = new();

        public frmMain()
        {
            InitializeComponent();
            SetupForm();
            CreateLayout();
            SetupMenu();
            SetupBackgroundTasks();
            LoadDashboard();
            LoadNotificationCount();
        }

        private void SetupForm()
        {
            this.Text = "NHÀ TRỌ PRO - Quản Lý Nhà Trọ Chuyên Nghiệp";
            this.Size = new Size(1450, 880);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 700);
            this.BackColor = ThemeManager.Background;
            this.DoubleBuffered = true;
            this.FormClosing += FrmMain_FormClosing;

            ThemeManager.IsDarkMode = AuthService.CurrentUser?.Theme == "Dark";
        }

        private void SetupBackgroundTasks()
        {
            // Chạy ngay khi khởi động (chỉ Admin/Manager)
            if (AuthService.IsAdmin || AuthService.IsManager)
            {
                Task.Run(async () => await RunBackgroundTasksAsync());

                // Timer chạy mỗi 6 giờ để kiểm tra
                _dailyTaskTimer = new System.Windows.Forms.Timer();
                _dailyTaskTimer.Interval = 6 * 60 * 60 * 1000; // 6 giờ
                _dailyTaskTimer.Tick += async (s, e) => await RunBackgroundTasksAsync();
                _dailyTaskTimer.Start();
            }
        }

        private async Task RunBackgroundTasksAsync()
        {
            try
            {
                // Tự động expire hợp đồng hết hạn
                await _backgroundService.AutoExpireContractsAsync();
                
                // Tự động hủy yêu cầu thuê phòng hết hạn
                await _backgroundService.AutoCancelExpiredBookingRequestsAsync();
            }
            catch (Exception ex)
            {
                // Log error silently, không hiện popup để không làm phiền user
                System.Diagnostics.Debug.WriteLine($"Background task error: {ex.Message}");
            }
        }

        private void FrmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _dailyTaskTimer?.Stop();
            _dailyTaskTimer?.Dispose();
        }

        private void CreateLayout()
        {
            // ===== SIDEBAR MODERN =====
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = ThemeManager.Sidebar,
                Tag = "sidebar"
            };

            // Logo Section
            var pnlLogo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 0, 20, 0)
            };

            var lblLogo = new Label
            {
                Text = "🏠 NHÀ TRỌ PRO",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlLogo.Controls.Add(lblLogo);

            // Separator
            var pnlSep = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(55, 65, 81)
            };

            // Menu Container with scroll
            pnlMenuContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 15, 12, 15)
            };

            pnlSidebar.Controls.Add(pnlMenuContainer);
            pnlSidebar.Controls.Add(pnlSep);
            pnlSidebar.Controls.Add(pnlLogo);

            // ===== TOPBAR MODERN =====
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = ThemeManager.Topbar,
                Padding = new Padding(25, 0, 25, 0)
            };

            // Add bottom border
            var pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ThemeManager.Border
            };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            lblTitle = new Label
            {
                Text = "📊 Dashboard",
                Font = new Font("Segoe UI Semibold", 18),
                ForeColor = ThemeManager.TextPrimary,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 400,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // User Info Panel - Right side
            var pnlUserSection = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                BackColor = Color.Transparent
            };

            // Theme Toggle - Modern round button
            btnThemeToggle = new Button
            {
                Text = ThemeManager.IsDarkMode ? "☀️" : "🌙",
                Size = new Size(42, 42),
                Location = new Point(10, 14),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand,
                BackColor = ThemeManager.SurfaceHover
            };
            btnThemeToggle.FlatAppearance.BorderSize = 1;
            btnThemeToggle.FlatAppearance.BorderColor = ThemeManager.Border;
            btnThemeToggle.Click += BtnThemeToggle_Click;
            UIHelper.RoundControl(btnThemeToggle, 21);

            // Notification Button
            var btnNotification = new Button
            {
                Text = "🔔",
                Size = new Size(42, 42),
                Location = new Point(62, 14),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand,
                BackColor = ThemeManager.SurfaceHover
            };
            btnNotification.FlatAppearance.BorderSize = 1;
            btnNotification.FlatAppearance.BorderColor = ThemeManager.Border;
            UIHelper.RoundControl(btnNotification, 21);

            lblNotificationBadge = new Label
            {
                Text = "0",
                Size = new Size(20, 20),
                Location = new Point(90, 10),
                BackColor = ThemeManager.Error,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            UIHelper.RoundControl(lblNotificationBadge, 10);

            // Avatar
            picAvatar = new PictureBox
            {
                Size = new Size(42, 42),
                Location = new Point(120, 14),
                BackColor = ThemeManager.Primary,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            UIHelper.RoundControl(picAvatar, 21);

            // Add avatar initial
            var avatarLabel = new Label
            {
                Text = AuthService.CurrentUser?.FullName?.Substring(0, 1).ToUpper() ?? "A",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            picAvatar.Controls.Add(avatarLabel);

            lblUserInfo = new Label
            {
                Text = $"{AuthService.CurrentUser?.FullName ?? "Admin"}\n{AuthService.CurrentUser?.RoleName ?? "Quản trị viên"}",
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeManager.TextPrimary,
                Location = new Point(170, 14),
                Size = new Size(120, 42),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnLogout = new Button
            {
                Text = "Đăng xuất",
                Size = new Size(90, 38),
                Location = new Point(290, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.Error,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click;
            UIHelper.RoundControl(btnLogout, 8);

            pnlUserSection.Controls.AddRange(new Control[] { btnThemeToggle, btnNotification, lblNotificationBadge, picAvatar, lblUserInfo, btnLogout });
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(pnlUserSection);

            // ===== CONTENT =====
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Background,
                Padding = new Padding(25)
            };

            // Add to form (order matters for docking)
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebar);
        }

        private void SetupMenu()
        {
            // Menu cho Tenant (riêng biệt)
            if (AuthService.IsTenant)
            {
                SetupTenantMenu();
                return;
            }

            // Menu cho Admin/Manager - Modern style
            var menuData = new List<(string Icon, string Text, string Tag, bool ForAdmin, bool ForManager)>
            {
                ("📊", "Dashboard", "dashboard", true, true),
                ("🏢", "Tòa nhà", "building", true, true),
                ("🚪", "Phòng trọ", "room", true, true),
                ("👥", "Khách thuê", "tenant", true, true),
                ("📝", "Hợp đồng", "contract", true, true),
                ("📋", "Yêu cầu thuê phòng", "booking_requests", true, true),
                ("💰", "Hóa đơn", "invoice", true, true),
                ("🔧", "Dịch vụ", "service", true, true),
                ("🛠", "Bảo trì", "maintenance", true, true),
                ("📈", "Báo cáo", "report", true, true),
                ("👤", "Tài khoản", "user", true, false),
                ("⚙️", "Cài đặt", "settings", true, true),
            };

            int y = 0;
            foreach (var item in menuData)
            {
                bool canAccess = (AuthService.IsAdmin && item.ForAdmin) ||
                                 (AuthService.IsManager && item.ForManager);

                if (!canAccess) continue;

                var menuPanel = CreateMenuItemPanel(item.Icon, item.Text, item.Tag, y);
                pnlMenuContainer.Controls.Add(menuPanel);
                menuItems.Add(menuPanel);
                y += 48;
            }
        }

        private void SetupTenantMenu()
        {
            var tenantMenuData = new List<(string Icon, string Text, string Tag)>
            {
                ("📊", "Dashboard", "tenant_dashboard"),
                ("👤", "Thông tin cá nhân", "my_profile"),
                ("🏠", "Phòng của tôi", "my_room"),
                ("🔍", "Tìm phòng trống", "available_rooms"),
                ("📋", "Yêu cầu thuê của tôi", "my_booking_requests"),
                ("💰", "Hóa đơn của tôi", "my_invoice"),
                ("📝", "Hợp đồng của tôi", "my_contract"),
                ("🛠", "Yêu cầu bảo trì", "my_ticket"),
                ("🔔", "Thông báo", "my_notification"),
            };

            int y = 0;
            foreach (var item in tenantMenuData)
            {
                var menuPanel = CreateMenuItemPanel(item.Icon, item.Text, item.Tag, y);
                pnlMenuContainer.Controls.Add(menuPanel);
                menuItems.Add(menuPanel);
                y += 48;
            }
        }

        private Panel CreateMenuItemPanel(string icon, string text, string tag, int y)
        {
            var panel = new Panel
            {
                Tag = tag,
                Location = new Point(0, y),
                Size = new Size(236, 44),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            UIHelper.RoundControl(panel, 10);

            // Active indicator bar (left side)
            var indicator = new Panel
            {
                Size = new Size(4, 28),
                Location = new Point(0, 8),
                BackColor = Color.Transparent,
                Tag = "indicator"
            };
            UIHelper.RoundControl(indicator, 2);

            var lbl = new Label
            {
                Text = $"  {icon}   {text}",
                Font = new Font("Segoe UI", 11),
                ForeColor = ThemeManager.SidebarText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            panel.Controls.Add(indicator);
            panel.Controls.Add(lbl);

            // Events
            panel.Click += MenuItem_Click;
            lbl.Click += (s, e) => MenuItem_Click(panel, e);

            panel.MouseEnter += (s, e) => {
                if (panel.BackColor != ThemeManager.SidebarActive)
                    panel.BackColor = ThemeManager.SidebarHover;
            };
            panel.MouseLeave += (s, e) => {
                if (panel.BackColor != ThemeManager.SidebarActive)
                    panel.BackColor = Color.Transparent;
            };
            lbl.MouseEnter += (s, e) => {
                if (panel.BackColor != ThemeManager.SidebarActive)
                    panel.BackColor = ThemeManager.SidebarHover;
            };
            lbl.MouseLeave += (s, e) => {
                if (panel.BackColor != ThemeManager.SidebarActive)
                    panel.BackColor = Color.Transparent;
            };

            return panel;
        }

        private void MenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not Panel panel) return;

            // Reset all menu items
            foreach (var item in menuItems)
            {
                item.BackColor = Color.Transparent;
                var indicator = item.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "indicator");
                if (indicator != null) indicator.BackColor = Color.Transparent;
                var lbl = item.Controls.OfType<Label>().FirstOrDefault();
                if (lbl != null) lbl.ForeColor = ThemeManager.SidebarText;
            }

            // Highlight selected
            panel.BackColor = ThemeManager.SidebarActive;
            UIHelper.RoundControl(panel, 10);
            var activeIndicator = panel.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "indicator");
            if (activeIndicator != null) activeIndicator.BackColor = Color.White;
            var activeLabel = panel.Controls.OfType<Label>().FirstOrDefault();
            if (activeLabel != null) activeLabel.ForeColor = Color.White;

            // Load content
            var tag = panel.Tag?.ToString();
            var menuText = activeLabel?.Text.Trim() ?? "";
            lblTitle.Text = menuText;

            pnlContent.Controls.Clear();
            var userId = AuthService.CurrentUser?.UserId ?? 0;

            switch (tag)
            {
                // Admin/Manager menu
                case "dashboard":
                    LoadDashboard();
                    break;
                case "building":
                    LoadUserControl(new ucBuilding());
                    break;
                case "room":
                    LoadUserControl(new ucPhongTro());
                    break;
                case "tenant":
                    LoadUserControl(new ucKhachThue());
                    break;
                case "contract":
                    LoadUserControl(new ucHopDong());
                    break;
                case "booking_requests":
                    LoadUserControl(new ucBookingRequests());
                    break;
                case "invoice":
                    LoadUserControl(new ucHoaDon());
                    break;
                case "service":
                    LoadUserControl(new ucDichVu());
                    break;
                case "maintenance":
                    LoadUserControl(new ucBaoTri());
                    break;
                case "report":
                    LoadUserControl(new ucReport());
                    break;
                case "user":
                    LoadUserControl(new ucUser());
                    break;
                case "settings":
                    LoadUserControl(new ucSettings());
                    break;

                // Tenant menu
                case "tenant_dashboard":
                    LoadUserControl(new ucTenantDashboard(userId));
                    break;
                case "my_profile":
                    LoadUserControl(new ucMyProfile(userId));
                    break;
                case "my_room":
                    LoadUserControl(new ucMyRoom(userId));
                    break;
                case "available_rooms":
                    LoadUserControl(new ucAvailableRooms(userId));
                    break;
                case "my_booking_requests":
                    LoadUserControl(new ucMyBookingRequests(userId));
                    break;
                case "my_invoice":
                    LoadUserControl(new ucMyInvoice(userId));
                    break;
                case "my_contract":
                    LoadUserControl(new ucMyContract(userId));
                    break;
                case "my_ticket":
                    LoadUserControl(new ucMyTicket(userId));
                    break;
                case "my_notification":
                    LoadUserControl(new ucTenantNotification(userId));
                    break;
            }
        }

        private void LoadDashboard()
        {
            lblTitle.Text = "📊  Dashboard";
            pnlContent.Controls.Clear();

            if (AuthService.IsTenant)
            {
                var userId = AuthService.CurrentUser?.UserId ?? 0;
                var tenantDashboard = new ucTenantDashboard(userId) { Dock = DockStyle.Fill };
                pnlContent.Controls.Add(tenantDashboard);
            }
            else
            {
                var dashboard = new ucDashboard { Dock = DockStyle.Fill };
                pnlContent.Controls.Add(dashboard);
            }

            // Highlight dashboard menu item
            if (menuItems.Count > 0)
            {
                var firstItem = menuItems[0];
                firstItem.BackColor = ThemeManager.SidebarActive;
                var indicator = firstItem.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "indicator");
                if (indicator != null) indicator.BackColor = Color.White;
                var lbl = firstItem.Controls.OfType<Label>().FirstOrDefault();
                if (lbl != null) lbl.ForeColor = Color.White;
            }
        }

        private void LoadUserControl(UserControl uc)
        {
            uc.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(uc);
        }

        private async void LoadNotificationCount()
        {
            try
            {
                var count = await _notiRepo.CountUnreadAsync(AuthService.CurrentUser?.UserId);
                if (count > 0)
                {
                    lblNotificationBadge.Text = count > 99 ? "99+" : count.ToString();
                    lblNotificationBadge.Visible = true;
                }
            }
            catch { }
        }

        private async void BtnThemeToggle_Click(object? sender, EventArgs e)
        {
            ThemeManager.ToggleTheme();
            btnThemeToggle.Text = ThemeManager.IsDarkMode ? "☀" : "🌙";

            // Save to database
            await _authService.UpdateThemeAsync(ThemeManager.IsDarkMode ? "Dark" : "Light");

            // Apply theme
            ThemeManager.ApplyTheme(this);
        }

        private async void BtnLogout_Click(object? sender, EventArgs e)
        {
            if (UIHelper.Confirm("Bạn có chắc muốn đăng xuất?"))
            {
                await _authService.LogoutAsync();
                this.Close();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(1400, 850);
            this.Name = "frmMain";
            this.ResumeLayout(false);
        }
    }
}
