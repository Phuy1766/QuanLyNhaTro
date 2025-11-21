using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class DichVuRepository : BaseRepository<DichVu>
    {
        protected override string GetTableName() => "DICHVU";
        protected override string GetPrimaryKey() => "DichVuId";

        protected override string GetInsertQuery() => @"
            INSERT INTO DICHVU (MaDichVu, TenDichVu, DonGia, DonViTinh, LoaiDichVu, MoTa, IsActive)
            VALUES (@MaDichVu, @TenDichVu, @DonGia, @DonViTinh, @LoaiDichVu, @MoTa, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE DICHVU SET
                MaDichVu = @MaDichVu, TenDichVu = @TenDichVu, DonGia = @DonGia,
                DonViTinh = @DonViTinh, LoaiDichVu = @LoaiDichVu, MoTa = @MoTa,
                IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE DichVuId = @DichVuId";

        /// <summary>
        /// Kiểm tra mã dịch vụ đã tồn tại chưa
        /// </summary>
        public async Task<bool> MaDichVuExistsAsync(string maDichVu, int? excludeId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM DICHVU WHERE MaDichVu = @MaDichVu";
            if (excludeId.HasValue)
                sql += " AND DichVuId != @ExcludeId";

            return await conn.ExecuteScalarAsync<int>(sql, new { MaDichVu = maDichVu, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Lấy dịch vụ theo loại
        /// </summary>
        public async Task<IEnumerable<DichVu>> GetByLoaiAsync(string loaiDichVu)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM DICHVU WHERE LoaiDichVu = @LoaiDichVu AND IsActive = 1 ORDER BY TenDichVu";
            return await conn.QueryAsync<DichVu>(sql, new { LoaiDichVu = loaiDichVu });
        }

        /// <summary>
        /// Lấy dịch vụ theo chỉ số (điện, nước)
        /// </summary>
        public async Task<IEnumerable<DichVu>> GetChiSoServicesAsync()
        {
            return await GetByLoaiAsync("TheoChiSo");
        }

        /// <summary>
        /// Lấy dịch vụ cố định
        /// </summary>
        public async Task<IEnumerable<DichVu>> GetFixedServicesAsync()
        {
            return await GetByLoaiAsync("CoDinh");
        }
    }
}
