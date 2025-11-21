using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class KhachThueRepository : BaseRepository<KhachThue>
    {
        protected override string GetTableName() => "KHACHTHUE";
        protected override string GetPrimaryKey() => "KhachId";

        protected override string GetInsertQuery() => @"
            INSERT INTO KHACHTHUE (MaKhach, HoTen, CCCD, NgaySinh, GioiTinh, DiaChi, Phone, Email, NgheNghiep, NoiLamViec, HinhAnh, UserId, IsActive)
            VALUES (@MaKhach, @HoTen, @CCCD, @NgaySinh, @GioiTinh, @DiaChi, @Phone, @Email, @NgheNghiep, @NoiLamViec, @HinhAnh, @UserId, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE KHACHTHUE SET
                MaKhach = @MaKhach, HoTen = @HoTen, CCCD = @CCCD, NgaySinh = @NgaySinh,
                GioiTinh = @GioiTinh, DiaChi = @DiaChi, Phone = @Phone, Email = @Email,
                NgheNghiep = @NgheNghiep, NoiLamViec = @NoiLamViec, HinhAnh = @HinhAnh,
                UserId = @UserId, IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE KhachId = @KhachId";

        /// <summary>
        /// Lấy danh sách khách thuê với thông tin phòng
        /// </summary>
        public async Task<IEnumerable<KhachThue>> GetAllWithRoomInfoAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT k.*, p.MaPhong, b.BuildingName, hd.TrangThai AS TrangThaiHopDong
                FROM KHACHTHUE k
                LEFT JOIN HOPDONG hd ON k.KhachId = hd.KhachId AND hd.TrangThai = N'Active'
                LEFT JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                WHERE k.IsActive = 1
                ORDER BY k.HoTen";
            return await conn.QueryAsync<KhachThue>(sql);
        }

        /// <summary>
        /// Lấy khách thuê chưa có hợp đồng active
        /// </summary>
        public async Task<IEnumerable<KhachThue>> GetAvailableTenantsAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT k.* FROM KHACHTHUE k
                WHERE k.IsActive = 1
                  AND NOT EXISTS (SELECT 1 FROM HOPDONG hd WHERE hd.KhachId = k.KhachId AND hd.TrangThai = N'Active')
                ORDER BY k.HoTen";
            return await conn.QueryAsync<KhachThue>(sql);
        }

        /// <summary>
        /// Kiểm tra CCCD đã tồn tại chưa
        /// </summary>
        public async Task<bool> CCCDExistsAsync(string cccd, int? excludeId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM KHACHTHUE WHERE CCCD = @CCCD";
            if (excludeId.HasValue)
                sql += " AND KhachId != @ExcludeId";

            return await conn.ExecuteScalarAsync<int>(sql, new { CCCD = cccd, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Kiểm tra mã khách đã tồn tại chưa
        /// </summary>
        public async Task<bool> MaKhachExistsAsync(string maKhach, int? excludeId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM KHACHTHUE WHERE MaKhach = @MaKhach";
            if (excludeId.HasValue)
                sql += " AND KhachId != @ExcludeId";

            return await conn.ExecuteScalarAsync<int>(sql, new { MaKhach = maKhach, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Kiểm tra khách có hợp đồng active không
        /// </summary>
        public async Task<bool> HasActiveContractAsync(int khachId)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM HOPDONG WHERE KhachId = @KhachId AND TrangThai = N'Active'";
            return await conn.ExecuteScalarAsync<int>(sql, new { KhachId = khachId }) > 0;
        }

        /// <summary>
        /// Tìm kiếm khách thuê
        /// </summary>
        public async Task<IEnumerable<KhachThue>> SearchAsync(string keyword)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT k.*, p.MaPhong, b.BuildingName, hd.TrangThai AS TrangThaiHopDong
                FROM KHACHTHUE k
                LEFT JOIN HOPDONG hd ON k.KhachId = hd.KhachId AND hd.TrangThai = N'Active'
                LEFT JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                WHERE k.IsActive = 1
                  AND (k.MaKhach LIKE @Keyword
                       OR k.HoTen LIKE @Keyword
                       OR k.CCCD LIKE @Keyword
                       OR k.Phone LIKE @Keyword)
                ORDER BY k.HoTen";
            return await conn.QueryAsync<KhachThue>(sql, new { Keyword = $"%{keyword}%" });
        }

        /// <summary>
        /// Lấy khách theo UserId
        /// </summary>
        public async Task<KhachThue?> GetByUserIdAsync(int userId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM KHACHTHUE WHERE UserId = @UserId AND IsActive = 1";
            return await conn.QueryFirstOrDefaultAsync<KhachThue>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Sinh mã khách tự động
        /// </summary>
        public async Task<string> GenerateMaKhachAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT TOP 1 MaKhach FROM KHACHTHUE
                WHERE MaKhach LIKE 'KH%'
                ORDER BY CAST(SUBSTRING(MaKhach, 3, 10) AS INT) DESC";
            var lastCode = await conn.QueryFirstOrDefaultAsync<string>(sql);

            if (string.IsNullOrEmpty(lastCode))
                return "KH001";

            var num = int.Parse(lastCode.Substring(2)) + 1;
            return $"KH{num:D3}";
        }
    }
}
