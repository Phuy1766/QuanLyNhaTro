using QuanLyNhaTro.DAL.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;

namespace QuanLyNhaTro.BLL.Services
{
    /// <summary>
    /// Background service tự động xử lý các tác vụ định kỳ
    /// </summary>
    public class BackgroundTaskService
    {
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly YeuCauThuePhongRepository _yeuCauRepo = new();

        /// <summary>
        /// Tự động chuyển hợp đồng hết hạn sang trạng thái Expired
        /// Nên chạy mỗi ngày lúc 0:00
        /// </summary>
        public async Task<(int ExpiredCount, int UpdatedRooms, string Message)> AutoExpireContractsAsync()
        {
            try
            {
                using var conn = new SqlConnection(DAL.DatabaseHelper.ConnectionString);
                var result = await conn.QueryFirstAsync<dynamic>(
                    "sp_AutoExpireContracts",
                    commandType: CommandType.StoredProcedure);

                return (
                    ExpiredCount: result.ExpiredCount ?? 0,
                    UpdatedRooms: result.UpdatedRooms ?? 0,
                    Message: result.Message?.ToString() ?? ""
                );
            }
            catch (Exception ex)
            {
                return (0, 0, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Tự động hủy yêu cầu thuê phòng hết hạn thanh toán
        /// Nên chạy mỗi giờ
        /// </summary>
        public async Task<(int CanceledCount, string Message)> AutoCancelExpiredBookingRequestsAsync()
        {
            try
            {
                using var conn = new SqlConnection(DAL.DatabaseHelper.ConnectionString);
                
                // Kiểm tra SP có tồn tại không
                var spExists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.procedures WHERE name = 'sp_AutoCancelExpiredBookingRequests'");
                
                if (spExists == 0)
                {
                    return (0, "Stored procedure sp_AutoCancelExpiredBookingRequests chưa được tạo");
                }

                var result = await conn.QueryFirstAsync<dynamic>(
                    "sp_AutoCancelExpiredBookingRequests",
                    new { HoursTimeout = 24 },
                    commandType: CommandType.StoredProcedure);

                return (
                    CanceledCount: result.CanceledCount ?? 0,
                    Message: result.Message?.ToString() ?? ""
                );
            }
            catch (Exception ex)
            {
                return (0, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo hóa đơn tự động cho tháng hiện tại
        /// Nên chạy vào ngày 1 hoặc 5 hàng tháng
        /// </summary>
        public async Task<(int Success, int Failed, string Message)> AutoCreateMonthlyInvoicesAsync()
        {
            try
            {
                var hoaDonService = new HoaDonService();
                var thangNam = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                
                return await hoaDonService.CreateBatchAsync(thangNam);
            }
            catch (Exception ex)
            {
                return (0, 0, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Gửi thông báo hợp đồng sắp hết hạn (7 ngày trước)
        /// Nên chạy mỗi ngày
        /// </summary>
        public async Task<(int NotificationCount, string Message)> SendContractExpirationNotificationsAsync()
        {
            try
            {
                using var conn = new SqlConnection(DAL.DatabaseHelper.ConnectionString);
                
                // Kiểm tra SP có tồn tại không
                var spExists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.procedures WHERE name = 'sp_CreateAutoNotifications'");
                
                if (spExists == 0)
                {
                    return (0, "Stored procedure sp_CreateAutoNotifications chưa được tạo");
                }

                await conn.ExecuteAsync("sp_CreateAutoNotifications", commandType: CommandType.StoredProcedure);
                
                return (1, "Đã kiểm tra và tạo thông báo tự động");
            }
            catch (Exception ex)
            {
                return (0, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Chạy tất cả các tác vụ background
        /// </summary>
        public async Task<string> RunAllTasksAsync()
        {
            var results = new List<string>();

            // 1. Expire hợp đồng
            var (expiredCount, updatedRooms, expireMsg) = await AutoExpireContractsAsync();
            results.Add($"✓ Expired {expiredCount} hợp đồng, cập nhật {updatedRooms} phòng");

            // 2. Hủy yêu cầu hết hạn
            var (canceledCount, cancelMsg) = await AutoCancelExpiredBookingRequestsAsync();
            results.Add($"✓ Hủy {canceledCount} yêu cầu hết hạn");

            // 3. Gửi thông báo
            var (notiCount, notiMsg) = await SendContractExpirationNotificationsAsync();
            results.Add($"✓ Kiểm tra thông báo: {notiMsg}");

            // 4. Tạo hóa đơn (chỉ vào ngày 1-5 hàng tháng)
            if (DateTime.Now.Day <= 5)
            {
                var (success, failed, invoiceMsg) = await AutoCreateMonthlyInvoicesAsync();
                results.Add($"✓ Tạo hóa đơn: {success} thành công, {failed} thất bại");
            }

            return string.Join("\n", results);
        }
    }
}
