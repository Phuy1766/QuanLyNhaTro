using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class TaiSanRepository : BaseRepository<TaiSan>
    {
        protected override string GetTableName() => "TAISAN";
        protected override string GetPrimaryKey() => "TaiSanId";

        protected override string GetInsertQuery() => @"
            INSERT INTO TAISAN (TenTaiSan, DonVi, GiaTri, MoTa, IsActive)
            VALUES (@TenTaiSan, @DonVi, @GiaTri, @MoTa, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE TAISAN SET
                TenTaiSan = @TenTaiSan, DonVi = @DonVi, GiaTri = @GiaTri,
                MoTa = @MoTa, IsActive = @IsActive
            WHERE TaiSanId = @TaiSanId";

        /// <summary>
        /// Lấy tài sản trong phòng
        /// </summary>
        public async Task<IEnumerable<TaiSanPhong>> GetByPhongAsync(int phongId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT tp.*, ts.TenTaiSan, ts.DonVi, ts.GiaTri
                FROM TAISAN_PHONG tp
                JOIN TAISAN ts ON tp.TaiSanId = ts.TaiSanId
                WHERE tp.PhongId = @PhongId
                ORDER BY ts.TenTaiSan";
            return await conn.QueryAsync<TaiSanPhong>(sql, new { PhongId = phongId });
        }

        /// <summary>
        /// Thêm tài sản vào phòng
        /// </summary>
        public async Task<int> AddToPhongAsync(TaiSanPhong taiSanPhong)
        {
            using var conn = GetConnection();
            var sql = @"
                INSERT INTO TAISAN_PHONG (PhongId, TaiSanId, SoLuong, TinhTrang, NgayNhap, GhiChu)
                VALUES (@PhongId, @TaiSanId, @SoLuong, @TinhTrang, @NgayNhap, @GhiChu);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            return await conn.ExecuteScalarAsync<int>(sql, taiSanPhong);
        }

        /// <summary>
        /// Cập nhật tài sản trong phòng
        /// </summary>
        public async Task<bool> UpdatePhongTaiSanAsync(TaiSanPhong taiSanPhong)
        {
            using var conn = GetConnection();
            var sql = @"
                UPDATE TAISAN_PHONG SET
                    SoLuong = @SoLuong, TinhTrang = @TinhTrang, GhiChu = @GhiChu
                WHERE TaiSanPhongId = @TaiSanPhongId";
            return await conn.ExecuteAsync(sql, taiSanPhong) > 0;
        }

        /// <summary>
        /// Xóa tài sản khỏi phòng
        /// </summary>
        public async Task<bool> RemoveFromPhongAsync(int taiSanPhongId)
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync(
                "DELETE FROM TAISAN_PHONG WHERE TaiSanPhongId = @Id",
                new { Id = taiSanPhongId }) > 0;
        }

        /// <summary>
        /// Tính tổng giá trị tài sản hỏng trong phòng
        /// </summary>
        public async Task<decimal> GetDamagedValueAsync(int phongId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT ISNULL(SUM(ts.GiaTri * tp.SoLuong), 0)
                FROM TAISAN_PHONG tp
                JOIN TAISAN ts ON tp.TaiSanId = ts.TaiSanId
                WHERE tp.PhongId = @PhongId AND tp.TinhTrang = N'Hỏng'";
            return await conn.ExecuteScalarAsync<decimal>(sql, new { PhongId = phongId });
        }

        /// <summary>
        /// Lấy tài sản trong phòng theo PhongId (cho Tenant)
        /// </summary>
        public async Task<IEnumerable<TaiSanPhong>> GetByPhongIdAsync(int phongId)
        {
            return await GetByPhongAsync(phongId);
        }
    }
}
