using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    /// <summary>
    /// Repository cho Ghi nhận hư hỏng tài sản
    /// FIX Issue 4.1: Ghi nhận + phê duyệt hư hỏng trước khi khấu trừ
    /// </summary>
    public class DamageReportRepository : BaseRepository<DamageReport>
    {
        protected override string GetTableName() => "DAMAGE_REPORT";
        protected override string GetPrimaryKey() => "DamageId";

        protected override string GetInsertQuery() => @"
            INSERT INTO DAMAGE_REPORT (HopDongId, PhongId, TaiSanId, MoTa, MucDoHuHong, GiaTriHuHong, HinhAnhChungCu, NguoiGhiNhan)
            VALUES (@HopDongId, @PhongId, @TaiSanId, @MoTa, @MucDoHuHong, @GiaTriHuHong, @HinhAnhChungCu, @NguoiGhiNhan);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE DAMAGE_REPORT SET
                TrangThai = @TrangThai, NgayPheDuyet = @NgayPheDuyet, NguoiPheDuyet = @NguoiPheDuyet,
                LyDoTuChoi = @LyDoTuChoi, GhiChu = @GhiChu, UpdatedAt = GETDATE()
            WHERE DamageId = @DamageId";

        /// <summary>
        /// Tạo ghi nhận hư hỏng mới
        /// </summary>
        public async Task<(bool Success, string Message, int DamageId)> CreateAsync(DamageReport report)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_CreateDamageReport",
                new
                {
                    HopDongId = report.HopDongId,
                    PhongId = report.PhongId,
                    TaiSanId = report.TaiSanId,
                    MoTa = report.MoTa,
                    MucDoHuHong = report.MucDoHuHong,
                    GiaTriHuHong = report.GiaTriHuHong,
                    HinhAnhChungCu = report.HinhAnhChungCu,
                    NguoiGhiNhan = report.NguoiGhiNhan
                },
                commandType: System.Data.CommandType.StoredProcedure);

            return (result.Success == 1, result.Message, result.DamageId ?? 0);
        }

        /// <summary>
        /// Lấy danh sách ghi nhận hư hỏng chờ phê duyệt
        /// </summary>
        public async Task<IEnumerable<DamageReport>> GetPendingAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT dr.*, p.MaPhong, ts.TenTaiSan, u1.FullName AS TenNguoiGhiNhan, u2.FullName AS TenNguoiPheDuyet
                FROM DAMAGE_REPORT dr
                INNER JOIN PHONGTRO p ON dr.PhongId = p.PhongId
                INNER JOIN TAISAN ts ON dr.TaiSanId = ts.TaiSanId
                LEFT JOIN USERS u1 ON dr.NguoiGhiNhan = u1.UserId
                LEFT JOIN USERS u2 ON dr.NguoiPheDuyet = u2.UserId
                WHERE dr.TrangThai = 'PendingApproval'
                ORDER BY dr.NgayGhiNhan DESC";
            return await conn.QueryAsync<DamageReport>(sql);
        }

        /// <summary>
        /// Lấy ghi nhận hư hỏng theo hợp đồng
        /// </summary>
        public async Task<IEnumerable<DamageReport>> GetByHopDongAsync(int hopDongId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT dr.*, p.MaPhong, ts.TenTaiSan, u1.FullName AS TenNguoiGhiNhan, u2.FullName AS TenNguoiPheDuyet
                FROM DAMAGE_REPORT dr
                INNER JOIN PHONGTRO p ON dr.PhongId = p.PhongId
                INNER JOIN TAISAN ts ON dr.TaiSanId = ts.TaiSanId
                LEFT JOIN USERS u1 ON dr.NguoiGhiNhan = u1.UserId
                LEFT JOIN USERS u2 ON dr.NguoiPheDuyet = u2.UserId
                WHERE dr.HopDongId = @HopDongId
                ORDER BY dr.NgayGhiNhan DESC";
            return await conn.QueryAsync<DamageReport>(sql, new { HopDongId = hopDongId });
        }

        /// <summary>
        /// Phê duyệt/Từ chối ghi nhận hư hỏng
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveAsync(int damageId, int adminId, bool isApproved, string? lyDoTuChoi = null)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_ApproveDamageReport",
                new
                {
                    DamageId = damageId,
                    NguoiPheDuyet = adminId,
                    IsApproved = isApproved ? 1 : 0,
                    LyDoTuChoi = lyDoTuChoi
                },
                commandType: System.Data.CommandType.StoredProcedure);

            return (result.Success == 1, result.Message);
        }

        /// <summary>
        /// Tính tổng giá trị hư hỏng được phê duyệt của hợp đồng
        /// (Dùng để tính khấu trừ khi thanh lý)
        /// </summary>
        public async Task<decimal> GetTotalApprovedDamageByContractAsync(int hopDongId)
        {
            using var conn = GetConnection();
            var total = await conn.ExecuteScalarAsync<decimal>(
                "SELECT dbo.fn_GetTotalApprovedDamageByContract(@HopDongId)",
                new { HopDongId = hopDongId });
            return total;
        }

        /// <summary>
        /// Lấy ghi nhận hư hỏng theo ID
        /// </summary>
        public override async Task<DamageReport?> GetByIdAsync(int damageId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT dr.*, p.MaPhong, ts.TenTaiSan, u1.FullName AS TenNguoiGhiNhan, u2.FullName AS TenNguoiPheDuyet
                FROM DAMAGE_REPORT dr
                INNER JOIN PHONGTRO p ON dr.PhongId = p.PhongId
                INNER JOIN TAISAN ts ON dr.TaiSanId = ts.TaiSanId
                LEFT JOIN USERS u1 ON dr.NguoiGhiNhan = u1.UserId
                LEFT JOIN USERS u2 ON dr.NguoiPheDuyet = u2.UserId
                WHERE dr.DamageId = @DamageId";
            return await conn.QueryFirstOrDefaultAsync<DamageReport>(sql, new { DamageId = damageId });
        }
    }
}
