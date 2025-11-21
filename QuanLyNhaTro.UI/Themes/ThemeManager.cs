namespace QuanLyNhaTro.UI.Themes
{
    /// <summary>
    /// Quản lý theme Light/Dark cho ứng dụng - Modern UI Design
    /// </summary>
    public static class ThemeManager
    {
        public static event Action? ThemeChanged;

        private static bool _isDarkMode = false;

        public static bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    ThemeChanged?.Invoke();
                }
            }
        }

        // Light Theme Colors - Modern Professional
        public static class Light
        {
            public static Color Background => Color.FromArgb(243, 244, 246);      // #F3F4F6 - Soft gray
            public static Color Surface => Color.White;                            // #FFFFFF
            public static Color SurfaceHover => Color.FromArgb(249, 250, 251);    // #F9FAFB
            public static Color Primary => Color.FromArgb(37, 99, 235);           // #2563EB - Blue
            public static Color PrimaryDark => Color.FromArgb(29, 78, 216);       // #1D4ED8 - Dark blue
            public static Color PrimaryLight => Color.FromArgb(59, 130, 246);     // #3B82F6 - Light blue
            public static Color Secondary => Color.FromArgb(100, 116, 139);       // #64748B
            public static Color TextPrimary => Color.FromArgb(17, 24, 39);        // #111827 - Dark text
            public static Color TextSecondary => Color.FromArgb(107, 114, 128);   // #6B7280 - Gray text
            public static Color TextMuted => Color.FromArgb(156, 163, 175);       // #9CA3AF
            public static Color Border => Color.FromArgb(229, 231, 235);          // #E5E7EB
            public static Color Success => Color.FromArgb(16, 185, 129);          // #10B981 - Emerald
            public static Color Warning => Color.FromArgb(245, 158, 11);          // #F59E0B - Amber
            public static Color Error => Color.FromArgb(239, 68, 68);             // #EF4444 - Red
            public static Color Info => Color.FromArgb(59, 130, 246);             // #3B82F6 - Blue
            public static Color Sidebar => Color.FromArgb(17, 24, 39);            // #111827 - Navy dark
            public static Color SidebarHover => Color.FromArgb(31, 41, 55);       // #1F2937
            public static Color SidebarActive => Color.FromArgb(37, 99, 235);     // #2563EB
            public static Color SidebarText => Color.FromArgb(229, 231, 235);     // #E5E7EB
            public static Color SidebarTextMuted => Color.FromArgb(156, 163, 175);// #9CA3AF
            public static Color Topbar => Color.FromArgb(249, 250, 251);          // #F9FAFB
            public static Color CardShadow => Color.FromArgb(30, 0, 0, 0);        // 12% black
        }

        // Dark Theme Colors - Modern Professional
        public static class Dark
        {
            public static Color Background => Color.FromArgb(17, 24, 39);         // #111827
            public static Color Surface => Color.FromArgb(31, 41, 55);            // #1F2937
            public static Color SurfaceHover => Color.FromArgb(55, 65, 81);       // #374151
            public static Color Primary => Color.FromArgb(59, 130, 246);          // #3B82F6
            public static Color PrimaryDark => Color.FromArgb(96, 165, 250);      // #60A5FA
            public static Color PrimaryLight => Color.FromArgb(37, 99, 235);      // #2563EB
            public static Color Secondary => Color.FromArgb(156, 163, 175);       // #9CA3AF
            public static Color TextPrimary => Color.FromArgb(243, 244, 246);     // #F3F4F6
            public static Color TextSecondary => Color.FromArgb(156, 163, 175);   // #9CA3AF
            public static Color TextMuted => Color.FromArgb(107, 114, 128);       // #6B7280
            public static Color Border => Color.FromArgb(55, 65, 81);             // #374151
            public static Color Success => Color.FromArgb(16, 185, 129);          // #10B981
            public static Color Warning => Color.FromArgb(245, 158, 11);          // #F59E0B
            public static Color Error => Color.FromArgb(239, 68, 68);             // #EF4444
            public static Color Info => Color.FromArgb(59, 130, 246);             // #3B82F6
            public static Color Sidebar => Color.FromArgb(15, 23, 42);            // #0F172A - Darker navy
            public static Color SidebarHover => Color.FromArgb(30, 41, 59);       // #1E293B
            public static Color SidebarActive => Color.FromArgb(37, 99, 235);     // #2563EB
            public static Color SidebarText => Color.FromArgb(229, 231, 235);     // #E5E7EB
            public static Color SidebarTextMuted => Color.FromArgb(148, 163, 184);// #94A3B8
            public static Color Topbar => Color.FromArgb(31, 41, 55);             // #1F2937
            public static Color CardShadow => Color.FromArgb(40, 0, 0, 0);        // 16% black
        }

        // Current Theme Properties
        public static Color Background => IsDarkMode ? Dark.Background : Light.Background;
        public static Color Surface => IsDarkMode ? Dark.Surface : Light.Surface;
        public static Color SurfaceHover => IsDarkMode ? Dark.SurfaceHover : Light.SurfaceHover;
        public static Color Primary => IsDarkMode ? Dark.Primary : Light.Primary;
        public static Color PrimaryDark => IsDarkMode ? Dark.PrimaryDark : Light.PrimaryDark;
        public static Color PrimaryLight => IsDarkMode ? Dark.PrimaryLight : Light.PrimaryLight;
        public static Color Secondary => IsDarkMode ? Dark.Secondary : Light.Secondary;
        public static Color TextPrimary => IsDarkMode ? Dark.TextPrimary : Light.TextPrimary;
        public static Color TextSecondary => IsDarkMode ? Dark.TextSecondary : Light.TextSecondary;
        public static Color TextMuted => IsDarkMode ? Dark.TextMuted : Light.TextMuted;
        public static Color Border => IsDarkMode ? Dark.Border : Light.Border;
        public static Color Success => IsDarkMode ? Dark.Success : Light.Success;
        public static Color Warning => IsDarkMode ? Dark.Warning : Light.Warning;
        public static Color Error => IsDarkMode ? Dark.Error : Light.Error;
        public static Color Info => IsDarkMode ? Dark.Info : Light.Info;
        public static Color Sidebar => IsDarkMode ? Dark.Sidebar : Light.Sidebar;
        public static Color SidebarHover => IsDarkMode ? Dark.SidebarHover : Light.SidebarHover;
        public static Color SidebarActive => IsDarkMode ? Dark.SidebarActive : Light.SidebarActive;
        public static Color SidebarText => IsDarkMode ? Dark.SidebarText : Light.SidebarText;
        public static Color SidebarTextMuted => IsDarkMode ? Dark.SidebarTextMuted : Light.SidebarTextMuted;
        public static Color Topbar => IsDarkMode ? Dark.Topbar : Light.Topbar;
        public static Color CardShadow => IsDarkMode ? Dark.CardShadow : Light.CardShadow;

        // Card accent colors
        public static Color CardBlue => Color.FromArgb(59, 130, 246);      // #3B82F6
        public static Color CardGreen => Color.FromArgb(16, 185, 129);     // #10B981
        public static Color CardPurple => Color.FromArgb(139, 92, 246);    // #8B5CF6
        public static Color CardPink => Color.FromArgb(236, 72, 153);      // #EC4899
        public static Color CardOrange => Color.FromArgb(245, 158, 11);    // #F59E0B
        public static Color CardRed => Color.FromArgb(239, 68, 68);        // #EF4444

        // Chart colors
        public static Color[] ChartColors => new[]
        {
            Color.FromArgb(59, 130, 246),   // Blue
            Color.FromArgb(16, 185, 129),   // Green
            Color.FromArgb(245, 158, 11),   // Orange
            Color.FromArgb(239, 68, 68),    // Red
            Color.FromArgb(139, 92, 246),   // Purple
            Color.FromArgb(236, 72, 153),   // Pink
        };

        /// <summary>
        /// Apply theme to form
        /// </summary>
        public static void ApplyTheme(Form form)
        {
            form.BackColor = Background;
            ApplyThemeToControls(form.Controls);
        }

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case Panel panel:
                        if (panel.Tag?.ToString() != "sidebar")
                            panel.BackColor = Surface;
                        break;

                    case Label label:
                        label.ForeColor = TextPrimary;
                        break;

                    case TextBox textBox:
                        textBox.BackColor = Surface;
                        textBox.ForeColor = TextPrimary;
                        break;

                    case DataGridView dgv:
                        ApplyThemeToDataGridView(dgv);
                        break;
                }

                if (control.HasChildren)
                    ApplyThemeToControls(control.Controls);
            }
        }

        public static void ApplyThemeToDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = Surface;
            dgv.GridColor = Border;
            dgv.DefaultCellStyle.BackColor = Surface;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = Primary;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = IsDarkMode
                ? Color.FromArgb(45, 55, 72)
                : Color.FromArgb(248, 250, 252);
        }

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }
    }
}
