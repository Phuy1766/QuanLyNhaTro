using System.Net;
using System.Net.Mail;
using System.Text;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class EmailService
    {
        private readonly CauHinhRepository _configRepo = new();
        private readonly HoaDonRepository _hoaDonRepo = new();

        /// <summary>
        /// Gửi email
        /// </summary>
        public async Task<(bool Success, string Message)> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var config = await _configRepo.GetSmtpConfigAsync();

                if (string.IsNullOrEmpty(config.Email) || string.IsNullOrEmpty(config.Password))
                    return (false, "Chưa cấu hình SMTP! Vui lòng cấu hình email gửi trong phần Cài đặt.");

                using var client = new SmtpClient(config.Host, config.Port)
                {
                    Credentials = new NetworkCredential(config.Email, config.Password),
                    EnableSsl = true
                };

                var message = new MailMessage(config.Email, to, subject, body)
                {
                    IsBodyHtml = isHtml
                };

                await client.SendMailAsync(message);
                return (true, "Gửi email thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Gửi email thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Gửi hóa đơn qua email
        /// </summary>
        public async Task<(bool Success, string Message)> SendInvoiceAsync(int hoaDonId)
        {
            var hoaDon = await _hoaDonRepo.GetWithDetailsAsync(hoaDonId);
            if (hoaDon == null)
                return (false, "Không tìm thấy hóa đơn!");

            if (string.IsNullOrEmpty(hoaDon.Email))
                return (false, "Khách thuê chưa có email!");

            var tenNhaTro = await _configRepo.GetValueAsync("TEN_NHA_TRO") ?? "Nhà Trọ";
            var body = GenerateInvoiceHtml(hoaDon, tenNhaTro);

            return await SendEmailAsync(hoaDon.Email, $"Hóa đơn {hoaDon.MaHoaDon} - {tenNhaTro}", body);
        }

        /// <summary>
        /// Gửi hóa đơn hàng loạt
        /// </summary>
        public async Task<(int Success, int Failed, string Message)> SendInvoicesBatchAsync(IEnumerable<int> hoaDonIds)
        {
            int success = 0, failed = 0;

            foreach (var id in hoaDonIds)
            {
                var result = await SendInvoiceAsync(id);
                if (result.Success) success++;
                else failed++;

                // Delay để tránh spam
                await Task.Delay(1000);
            }

            return (success, failed, $"Đã gửi {success} email, {failed} thất bại");
        }

        /// <summary>
        /// Tạo HTML hóa đơn
        /// </summary>
        private static string GenerateInvoiceHtml(HoaDon hoaDon, string tenNhaTro)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { text-align: center; margin-bottom: 30px; }
        .header h1 { color: #2c3e50; margin: 0; }
        .header p { color: #7f8c8d; }
        .invoice-info { margin-bottom: 20px; }
        .invoice-info table { width: 100%; }
        .invoice-info td { padding: 5px 0; }
        .items { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
        .items th, .items td { border: 1px solid #ddd; padding: 10px; text-align: left; }
        .items th { background-color: #3498db; color: white; }
        .items tr:nth-child(even) { background-color: #f9f9f9; }
        .total { text-align: right; font-size: 18px; }
        .total-row { font-weight: bold; color: #e74c3c; }
        .footer { margin-top: 30px; text-align: center; color: #7f8c8d; font-size: 12px; }
        .amount { text-align: right; }
    </style>
</head>
<body>");

            sb.AppendLine($@"
    <div class='header'>
        <h1>{tenNhaTro}</h1>
        <p>HÓA ĐƠN TIỀN PHÒNG</p>
    </div>

    <div class='invoice-info'>
        <table>
            <tr>
                <td><strong>Mã hóa đơn:</strong> {hoaDon.MaHoaDon}</td>
                <td><strong>Ngày tạo:</strong> {hoaDon.NgayTao:dd/MM/yyyy}</td>
            </tr>
            <tr>
                <td><strong>Phòng:</strong> {hoaDon.MaPhong}</td>
                <td><strong>Tháng:</strong> {hoaDon.ThangNam:MM/yyyy}</td>
            </tr>
            <tr>
                <td><strong>Khách thuê:</strong> {hoaDon.TenKhachThue}</td>
                <td><strong>Hạn thanh toán:</strong> {hoaDon.NgayHetHan:dd/MM/yyyy}</td>
            </tr>
        </table>
    </div>

    <table class='items'>
        <thead>
            <tr>
                <th>STT</th>
                <th>Nội dung</th>
                <th>Chỉ số cũ</th>
                <th>Chỉ số mới</th>
                <th>Số lượng</th>
                <th>Đơn giá</th>
                <th class='amount'>Thành tiền</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>1</td>
                <td>Tiền phòng</td>
                <td>-</td>
                <td>-</td>
                <td>1 tháng</td>
                <td>{hoaDon.TienPhong:N0}</td>
                <td class='amount'>{hoaDon.TienPhong:N0}</td>
            </tr>");

            int stt = 2;
            foreach (var ct in hoaDon.ChiTietDichVu)
            {
                sb.AppendLine($@"
            <tr>
                <td>{stt++}</td>
                <td>{ct.TenDichVu}</td>
                <td>{ct.ChiSoCu?.ToString("N0") ?? "-"}</td>
                <td>{ct.ChiSoMoi?.ToString("N0") ?? "-"}</td>
                <td>{ct.SoLuong?.ToString("N0") ?? "1"} {ct.DonViTinh}</td>
                <td>{ct.DonGia:N0}</td>
                <td class='amount'>{ct.ThanhTien:N0}</td>
            </tr>");
            }

            sb.AppendLine($@"
        </tbody>
    </table>

    <div class='total'>
        <p>Tiền phòng: <strong>{hoaDon.TienPhong:N0} VNĐ</strong></p>
        <p>Tiền dịch vụ: <strong>{hoaDon.TongTienDichVu:N0} VNĐ</strong></p>
        <p class='total-row'>TỔNG CỘNG: {hoaDon.TongCong:N0} VNĐ</p>
        <p>Đã thanh toán: {hoaDon.DaThanhToan:N0} VNĐ</p>
        <p class='total-row'>CÒN NỢ: {hoaDon.ConNo:N0} VNĐ</p>
    </div>

    <div class='footer'>
        <p>Cảm ơn quý khách đã sử dụng dịch vụ của chúng tôi!</p>
        <p>Vui lòng thanh toán trước ngày {hoaDon.NgayHetHan:dd/MM/yyyy}</p>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Test kết nối SMTP
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                var config = await _configRepo.GetSmtpConfigAsync();

                if (string.IsNullOrEmpty(config.Email))
                    return (false, "Chưa cấu hình email!");

                using var client = new SmtpClient(config.Host, config.Port)
                {
                    Credentials = new NetworkCredential(config.Email, config.Password),
                    EnableSsl = true,
                    Timeout = 10000
                };

                // Test bằng cách gửi email cho chính mình
                var message = new MailMessage(config.Email, config.Email, "Test Connection", "Kết nối SMTP thành công!");
                await client.SendMailAsync(message);

                return (true, "Kết nối SMTP thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Kết nối thất bại: {ex.Message}");
            }
        }
    }
}
