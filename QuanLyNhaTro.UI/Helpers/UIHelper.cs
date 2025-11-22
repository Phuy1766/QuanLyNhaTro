using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace QuanLyNhaTro.UI.Helpers
{
    /// <summary>
    /// Helper class cho các thao tác UI phổ biến
    /// </summary>
    public static class UIHelper
    {
        // Windows API for rounded corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        /// <summary>
        /// Làm tròn góc Control
        /// </summary>
        public static void RoundControl(Control control, int radius)
        {
            control.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, control.Width + 1, control.Height + 1, radius, radius));
        }

        /// <summary>
        /// Hiển thị thông báo lỗi
        /// </summary>
        public static void ShowError(string message, string title = "Lỗi")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Hiển thị thông báo thành công
        /// </summary>
        public static void ShowSuccess(string message, string title = "Thành công")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hiển thị thông báo cảnh báo
        /// </summary>
        public static void ShowWarning(string message, string title = "Cảnh báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Hiển thị hộp thoại xác nhận
        /// </summary>
        public static bool Confirm(string message, string title = "Xác nhận")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Format số tiền VND
        /// </summary>
        public static string FormatCurrency(decimal amount)
        {
            return string.Format("{0:N0} VNĐ", amount);
        }

        /// <summary>
        /// Format ngày tháng
        /// </summary>
        public static string FormatDate(DateTime? date)
        {
            return date?.ToString("dd/MM/yyyy") ?? "";
        }

        /// <summary>
        /// Style DataGridView chuyên nghiệp
        /// </summary>
        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.RowHeadersVisible = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Header style
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(59, 130, 246);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            dgv.ColumnHeadersHeight = 45;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;

            // Row style
            dgv.DefaultCellStyle.Padding = new Padding(10, 8, 10, 8);
            dgv.RowTemplate.Height = 40;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            // Selection style
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
        }

        /// <summary>
        /// Thêm cột vào DataGridView
        /// </summary>
        public static void AddColumn(DataGridView dgv, string name, string headerText, string dataPropertyName,
            int width = 100, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                DataPropertyName = dataPropertyName,
                Width = width,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment }
            });
        }

        /// <summary>
        /// Thêm nút vào DataGridView
        /// </summary>
        public static void AddButtonColumn(DataGridView dgv, string name, string headerText, string buttonText, int width = 80)
        {
            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = headerText,
                Text = buttonText,
                UseColumnTextForButtonValue = true,
                Width = width,
                FlatStyle = FlatStyle.Flat
            });
        }

        /// <summary>
        /// Bind ComboBox với datasource
        /// </summary>
        public static void BindComboBox<T>(ComboBox cbo, IEnumerable<T> dataSource, string displayMember, string valueMember, bool addEmpty = true)
        {
            var list = dataSource.ToList();
            if (addEmpty && typeof(T).IsClass)
            {
                // Thêm item trống ở đầu
                cbo.Items.Clear();
                cbo.Items.Add("-- Chọn --");
            }

            cbo.DataSource = list;
            cbo.DisplayMember = displayMember;
            cbo.ValueMember = valueMember;
        }

        /// <summary>
        /// Lấy màu theo trạng thái
        /// </summary>
        public static Color GetStatusColor(string status)
        {
            return status switch
            {
                "Trống" or "Active" or "DaThanhToan" or "Hoàn thành" or "Tốt" => Color.FromArgb(34, 197, 94),
                "Đang thuê" or "ChuaThanhToan" or "Đang xử lý" => Color.FromArgb(59, 130, 246),
                "Đang sửa" or "Mới" or "Trung bình" => Color.FromArgb(234, 179, 8),
                "Expired" or "QuaHan" or "Hỏng" or "Khẩn cấp" => Color.FromArgb(239, 68, 68),
                "Terminated" or "Hủy" => Color.FromArgb(100, 116, 139),
                _ => Color.FromArgb(100, 116, 139)
            };
        }

        /// <summary>
        /// Tạo Panel với border radius
        /// </summary>
        public static Panel CreateCard(int x, int y, int width, int height)
        {
            return new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.White,
                Padding = new Padding(15),
            };
        }
                /// <summary>
        /// Áp dụng hiệu ứng bóng đổ cho Card (giả lập shadow đẹp)
        /// </summary>
        public static void ApplyCardShadow(Control control)
        {
            // Bóng đổ giả lập bằng vẽ viền mờ
            control.Paint += (sender, e) =>
            {
                var rect = new Rectangle(0, 0, control.Width, control.Height);
                using var pen1 = new Pen(Color.FromArgb(25, 0, 0, 0), 20);
                using var pen2 = new Pen(Color.FromArgb(15, 0, 0, 0), 35);
                using var pen3 = new Pen(Color.FromArgb(8, 0, 0, 0), 50);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen3, rect);
                e.Graphics.DrawRectangle(pen2, rect);
                e.Graphics.DrawRectangle(pen1, rect);
            };

            // Tạo độ nổi bằng margin
            control.Margin = new Padding(15);
        }

        /// <summary>
        /// Thêm cột vào DataGridView (phiên bản rút gọn, dùng trong ucMyInvoice)
        /// </summary>
        public static void AddColumn(DataGridView dgv, string name, string header, string dataProperty, int width)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProperty,
                Width = width,
                MinimumWidth = 80,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(12, 10, 12, 10),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                }
            };
            dgv.Columns.Add(col);
        }

    }
}
