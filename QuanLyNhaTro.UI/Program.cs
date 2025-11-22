using QuanLyNhaTro.DAL;
using QuanLyNhaTro.UI.Forms;

namespace QuanLyNhaTro.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Khởi tạo connection string
            // Thay đổi theo cấu hình SQL Server của bạn
            DatabaseHelper.Initialize(
                server: "DESKTOP-I2SES3S",  // SQL Server 2022
                database: "QuanLyNhaTro",
                integratedSecurity: true
            );

            // Test connection
            if (!DatabaseHelper.TestConnection(out string error))
            {
                MessageBox.Show(
                    $"Không thể kết nối database!\n\nLỗi: {error}\n\nVui lòng kiểm tra:\n" +
                    "1. SQL Server đã khởi động\n" +
                    "2. Database 'QuanLyNhaTro' đã được tạo\n" +
                    "3. Connection string đúng",
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new frmLogin());
        }
    }
}
