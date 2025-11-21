using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class NotificationRepository
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        /// <summary>
        /// Lấy thông báo chưa đọc của user
        /// </summary>
        public async Task<IEnumerable<Notification>> GetUnreadAsync(int? userId = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT * FROM NOTIFICATION_LOG
                WHERE (UserId = @UserId OR UserId IS NULL) AND DaDoc = 0
                ORDER BY NgayTao DESC";
            return await conn.QueryAsync<Notification>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Lấy tất cả thông báo của user
        /// </summary>
        public async Task<IEnumerable<Notification>> GetAllAsync(int? userId = null, int top = 50)
        {
            using var conn = GetConnection();
            var sql = $@"
                SELECT TOP {top} * FROM NOTIFICATION_LOG
                WHERE UserId = @UserId OR UserId IS NULL
                ORDER BY NgayTao DESC";
            return await conn.QueryAsync<Notification>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Đánh dấu đã đọc
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            using var conn = GetConnection();
            var sql = "UPDATE NOTIFICATION_LOG SET DaDoc = 1, NgayDoc = GETDATE() WHERE NotificationId = @Id";
            return await conn.ExecuteAsync(sql, new { Id = notificationId }) > 0;
        }

        /// <summary>
        /// Đánh dấu tất cả đã đọc
        /// </summary>
        public async Task<int> MarkAllAsReadAsync(int? userId = null)
        {
            using var conn = GetConnection();
            var sql = "UPDATE NOTIFICATION_LOG SET DaDoc = 1, NgayDoc = GETDATE() WHERE (UserId = @UserId OR UserId IS NULL) AND DaDoc = 0";
            return await conn.ExecuteAsync(sql, new { UserId = userId });
        }

        /// <summary>
        /// Thêm thông báo mới
        /// </summary>
        public async Task<int> AddAsync(Notification notification)
        {
            using var conn = GetConnection();
            var sql = @"
                INSERT INTO NOTIFICATION_LOG (UserId, LoaiThongBao, TieuDe, NoiDung, DuongDan)
                VALUES (@UserId, @LoaiThongBao, @TieuDe, @NoiDung, @DuongDan);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            return await conn.ExecuteScalarAsync<int>(sql, notification);
        }

        /// <summary>
        /// Đếm thông báo chưa đọc
        /// </summary>
        public async Task<int> CountUnreadAsync(int? userId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(*) FROM NOTIFICATION_LOG WHERE (UserId = @UserId OR UserId IS NULL) AND DaDoc = 0";
            return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Tạo thông báo tự động (hợp đồng sắp hết hạn, hóa đơn quá hạn...)
        /// </summary>
        public async Task CreateAutoNotificationsAsync()
        {
            using var conn = GetConnection();
            await conn.ExecuteAsync("EXEC sp_CreateAutoNotifications");
        }

        /// <summary>
        /// Xóa thông báo cũ
        /// </summary>
        public async Task<int> DeleteOldAsync(int days = 30)
        {
            using var conn = GetConnection();
            var sql = "DELETE FROM NOTIFICATION_LOG WHERE NgayTao < DATEADD(DAY, -@Days, GETDATE()) AND DaDoc = 1";
            return await conn.ExecuteAsync(sql, new { Days = days });
        }
    }
}
