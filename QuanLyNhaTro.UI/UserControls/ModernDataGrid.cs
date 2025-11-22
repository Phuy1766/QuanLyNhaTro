using System.Drawing;
using System.Windows.Forms;

namespace QuanLyNhaTro.UI.UserControls
{
    public class ModernDataGrid : DataGridView
    {
        public ModernDataGrid()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.GridColor = Color.FromArgb(228, 231, 240);
            this.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this.BackgroundColor = Color.White;
            this.BorderStyle = BorderStyle.None;
        }

        // Hiển thị dòng chữ khi không có dữ liệu
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (this.Rows.Count == 0 && this.Visible)
            {
                string text = "Không có hóa đơn nào";
                using var brush = new SolidBrush(Color.FromArgb(160, 160, 160));
                var size = e.Graphics.MeasureString(text, new Font("Segoe UI", 15F));
                e.Graphics.DrawString(text, new Font("Segoe UI", 15F), brush,
                    new PointF((this.Width - size.Width) / 2, (this.Height - size.Height) / 2));
            }
        }
    }
}