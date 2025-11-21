using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    /// <summary>
    /// Repository cho thanh toán cọc
    /// </summary>
    public class PaymentRepository
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        #region Payment Config

        /// <summary>
        /// Lấy danh sách cấu hình thanh toán
        /// </summary>
        public async Task<IEnumerable<PaymentConfig>> GetAllConfigsAsync(bool activeOnly = true)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM PAYMENT_CONFIG WHERE (@ActiveOnly = 0 OR IsActive = 1) ORDER BY ConfigId";
            return await conn.QueryAsync<PaymentConfig>(sql, new { ActiveOnly = activeOnly ? 1 : 0 });
        }

        /// <summary>
        /// Lấy cấu hình thanh toán mặc định
        /// </summary>
        public async Task<PaymentConfig?> GetDefaultConfigAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<PaymentConfig>(
                "SELECT TOP 1 * FROM PAYMENT_CONFIG WHERE IsActive = 1 ORDER BY ConfigId");
        }

        /// <summary>
        /// Lấy cấu hình theo ID
        /// </summary>
        public async Task<PaymentConfig?> GetConfigByIdAsync(int configId)
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<PaymentConfig>(
                "SELECT * FROM PAYMENT_CONFIG WHERE ConfigId = @ConfigId", new { ConfigId = configId });
        }

        #endregion

        #region Booking Payment

        /// <summary>
        /// Tạo yêu cầu thuê phòng + phiếu thanh toán
        /// </summary>
        public async Task<CreateBookingResult> CreateBookingWithPaymentAsync(
            int phongId, int maTenant, DateTime ngayBatDau, int soNguoi, string? ghiChu = null, int? bankConfigId = null)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<CreateBookingResult>(
                "sp_CreateBookingWithPayment",
                new { PhongId = phongId, MaTenant = maTenant, NgayBatDauMongMuon = ngayBatDau, SoNguoi = soNguoi, GhiChu = ghiChu, BankConfigId = bankConfigId },
                commandType: System.Data.CommandType.StoredProcedure);
            return result;
        }

        /// <summary>
        /// Lấy phiếu thanh toán theo mã yêu cầu
        /// </summary>
        public async Task<BookingPayment?> GetPaymentByYeuCauAsync(int maYeuCau)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT bp.*, p.MaPhong, b.BuildingName AS TenToaNha, u.FullName AS TenTenant, u.Phone AS SdtTenant,
                       ph.GiaThue AS GiaPhong, pc.BankName, pc.AccountName, pc.AccountNumber
                FROM BOOKING_PAYMENT bp
                INNER JOIN YEUCAU_THUEPHONG yc ON bp.MaYeuCau = yc.MaYeuCau
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN USERS u ON yc.MaTenant = u.UserId
                LEFT JOIN PHONGTRO ph ON yc.PhongId = ph.PhongId
                LEFT JOIN PAYMENT_CONFIG pc ON bp.BankConfigId = pc.ConfigId
                WHERE bp.MaYeuCau = @MaYeuCau";
            return await conn.QueryFirstOrDefaultAsync<BookingPayment>(sql, new { MaYeuCau = maYeuCau });
        }

        /// <summary>
        /// Lấy phiếu thanh toán theo ID
        /// </summary>
        public async Task<BookingPayment?> GetPaymentByIdAsync(int maThanhToan)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT bp.*, p.MaPhong, b.BuildingName AS TenToaNha, u.FullName AS TenTenant, u.Phone AS SdtTenant,
                       ph.GiaThue AS GiaPhong, pc.BankName, pc.AccountName, pc.AccountNumber,
                       xu.FullName AS TenNguoiXacNhan
                FROM BOOKING_PAYMENT bp
                INNER JOIN YEUCAU_THUEPHONG yc ON bp.MaYeuCau = yc.MaYeuCau
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN USERS u ON yc.MaTenant = u.UserId
                LEFT JOIN PHONGTRO ph ON yc.PhongId = ph.PhongId
                LEFT JOIN PAYMENT_CONFIG pc ON bp.BankConfigId = pc.ConfigId
                LEFT JOIN USERS xu ON bp.NguoiXacNhan = xu.UserId
                WHERE bp.MaThanhToan = @MaThanhToan";
            return await conn.QueryFirstOrDefaultAsync<BookingPayment>(sql, new { MaThanhToan = maThanhToan });
        }

        /// <summary>
        /// Tenant báo đã thanh toán
        /// </summary>
        public async Task<(bool Success, string Message)> ConfirmPaymentByTenantAsync(int maThanhToan, int maTenant)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_ConfirmPaymentByTenant",
                new { MaThanhToan = maThanhToan, MaTenant = maTenant },
                commandType: System.Data.CommandType.StoredProcedure);
            return (result.Success == 1, result.Message);
        }

        /// <summary>
        /// Admin xác nhận thanh toán
        /// </summary>
        public async Task<(bool Success, string Message)> AdminConfirmPaymentAsync(int maThanhToan, int nguoiXacNhan, bool isConfirmed, string? ghiChu = null)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_AdminConfirmPayment",
                new { MaThanhToan = maThanhToan, NguoiXacNhan = nguoiXacNhan, IsConfirmed = isConfirmed, GhiChu = ghiChu },
                commandType: System.Data.CommandType.StoredProcedure);
            return (result.Success == 1, result.Message);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu thuê với thông tin thanh toán (cho Admin)
        /// </summary>
        public async Task<IEnumerable<BookingRequestDTO>> GetAllBookingRequestsAsync(string? trangThai = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT yc.MaYeuCau, yc.PhongId, p.MaPhong, b.BuildingName AS TenToaNha,
                       yc.MaTenant, u.FullName AS TenTenant, u.Phone AS SdtTenant,
                       yc.NgayGui, yc.NgayBatDauMongMuon, yc.SoNguoi, yc.GhiChu,
                       yc.TrangThai, yc.NgayXuLy, xu.FullName AS TenNguoiXuLy, yc.LyDoTuChoi,
                       p.GiaThue AS GiaPhong, p.SoNguoiToiDa, p.DienTich,
                       bp.MaThanhToan, bp.SoTien AS SoTienCoc, bp.TrangThai AS TrangThaiThanhToan, bp.NgayThanhToan
                FROM YEUCAU_THUEPHONG yc
                INNER JOIN USERS u ON yc.MaTenant = u.UserId
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN USERS xu ON yc.NguoiXuLy = xu.UserId
                LEFT JOIN BOOKING_PAYMENT bp ON yc.MaYeuCau = bp.MaYeuCau
                WHERE (@TrangThai IS NULL OR yc.TrangThai = @TrangThai)
                ORDER BY
                    CASE yc.TrangThai
                        WHEN 'WaitingConfirm' THEN 1
                        WHEN 'PendingPayment' THEN 2
                        WHEN 'PendingApprove' THEN 3
                        WHEN 'Approved' THEN 4
                        ELSE 5
                    END,
                    yc.NgayGui DESC";
            return await conn.QueryAsync<BookingRequestDTO>(sql, new { TrangThai = trangThai });
        }

        /// <summary>
        /// Lấy yêu cầu thuê của Tenant
        /// </summary>
        public async Task<IEnumerable<BookingRequestDTO>> GetBookingsByTenantAsync(int tenantUserId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT yc.MaYeuCau, yc.PhongId, p.MaPhong, b.BuildingName AS TenToaNha,
                       yc.MaTenant, yc.NgayGui, yc.NgayBatDauMongMuon, yc.SoNguoi, yc.GhiChu,
                       yc.TrangThai, yc.NgayXuLy, yc.LyDoTuChoi,
                       p.GiaThue AS GiaPhong, p.SoNguoiToiDa, p.DienTich,
                       bp.MaThanhToan, bp.SoTien AS SoTienCoc, bp.TrangThai AS TrangThaiThanhToan, bp.NgayThanhToan
                FROM YEUCAU_THUEPHONG yc
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN BOOKING_PAYMENT bp ON yc.MaYeuCau = bp.MaYeuCau
                WHERE yc.MaTenant = @TenantUserId
                ORDER BY yc.NgayGui DESC";
            return await conn.QueryAsync<BookingRequestDTO>(sql, new { TenantUserId = tenantUserId });
        }

        /// <summary>
        /// Đếm số yêu cầu cần xử lý
        /// </summary>
        public async Task<(int WaitingConfirm, int PendingApprove)> CountPendingRequestsAsync()
        {
            using var conn = GetConnection();
            var waitingConfirm = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM YEUCAU_THUEPHONG WHERE TrangThai = 'WaitingConfirm'");
            var pendingApprove = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM YEUCAU_THUEPHONG WHERE TrangThai = 'PendingApprove'");
            return (waitingConfirm, pendingApprove);
        }

        #endregion
    }
}
