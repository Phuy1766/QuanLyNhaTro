using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;
using System.Text.Json;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class ActivityLogRepository
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        /// <summary>
        /// Ghi log hoạt động
        /// </summary>
        public async Task<int> LogAsync(int? userId, string tenBang, string? maBanGhi, string hanhDong,
            object? duLieuCu = null, object? duLieuMoi = null, string? moTa = null)
        {
            using var conn = GetConnection();
            var sql = @"
                INSERT INTO ACTIVITY_LOG (UserId, TenBang, MaBanGhi, HanhDong, DuLieuCu, DuLieuMoi, MoTa, IpAddress)
                VALUES (@UserId, @TenBang, @MaBanGhi, @HanhDong, @DuLieuCu, @DuLieuMoi, @MoTa, @IpAddress);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                TenBang = tenBang,
                MaBanGhi = maBanGhi,
                HanhDong = hanhDong,
                DuLieuCu = duLieuCu != null ? JsonSerializer.Serialize(duLieuCu) : null,
                DuLieuMoi = duLieuMoi != null ? JsonSerializer.Serialize(duLieuMoi) : null,
                MoTa = moTa,
                IpAddress = Environment.MachineName
            });
        }

        /// <summary>
        /// Lấy log theo thời gian
        /// </summary>
        public async Task<IEnumerable<ActivityLog>> GetAsync(DateTime? fromDate = null, DateTime? toDate = null,
            int? userId = null, string? tenBang = null, int top = 100)
        {
            using var conn = GetConnection();
            var sql = $@"
                SELECT TOP {top} al.*, u.FullName AS TenNguoiDung
                FROM ACTIVITY_LOG al
                LEFT JOIN USERS u ON al.UserId = u.UserId
                WHERE (@FromDate IS NULL OR al.NgayThucHien >= @FromDate)
                  AND (@ToDate IS NULL OR al.NgayThucHien <= @ToDate)
                  AND (@UserId IS NULL OR al.UserId = @UserId)
                  AND (@TenBang IS NULL OR al.TenBang = @TenBang)
                ORDER BY al.NgayThucHien DESC";

            return await conn.QueryAsync<ActivityLog>(sql, new
            {
                FromDate = fromDate,
                ToDate = toDate?.AddDays(1),
                UserId = userId,
                TenBang = tenBang
            });
        }

        /// <summary>
        /// Lấy log của một bản ghi cụ thể
        /// </summary>
        public async Task<IEnumerable<ActivityLog>> GetByRecordAsync(string tenBang, string maBanGhi)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT al.*, u.FullName AS TenNguoiDung
                FROM ACTIVITY_LOG al
                LEFT JOIN USERS u ON al.UserId = u.UserId
                WHERE al.TenBang = @TenBang AND al.MaBanGhi = @MaBanGhi
                ORDER BY al.NgayThucHien DESC";

            return await conn.QueryAsync<ActivityLog>(sql, new { TenBang = tenBang, MaBanGhi = maBanGhi });
        }

        /// <summary>
        /// Xóa log cũ
        /// </summary>
        public async Task<int> DeleteOldAsync(int days = 90)
        {
            using var conn = GetConnection();
            var sql = "DELETE FROM ACTIVITY_LOG WHERE NgayThucHien < DATEADD(DAY, -@Days, GETDATE())";
            return await conn.ExecuteAsync(sql, new { Days = days });
        }
    }
}
