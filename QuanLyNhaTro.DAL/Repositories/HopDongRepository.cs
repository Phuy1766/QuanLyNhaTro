using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class HopDongRepository : BaseRepository<HopDong>
    {
        protected override string GetTableName() => "HOPDONG";
        protected override string GetPrimaryKey() => "HopDongId";

        protected override string GetInsertQuery() => @"
            INSERT INTO HOPDONG (MaHopDong, PhongId, KhachId, NgayBatDau, NgayKetThuc, GiaThue, TienCoc, ChuKyThanhToan, TrangThai, GhiChu, CreatedBy)
            VALUES (@MaHopDong, @PhongId, @KhachId, @NgayBatDau, @NgayKetThuc, @GiaThue, @TienCoc, @ChuKyThanhToan, @TrangThai, @GhiChu, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE HOPDONG SET
                NgayKetThuc = @NgayKetThuc, TienCoc = @TienCoc, ChuKyThanhToan = @ChuKyThanhToan,
                TrangThai = @TrangThai, NgayThanhLy = @NgayThanhLy, TienHoanCoc = @TienHoanCoc,
                TienKhauTru = @TienKhauTru, LyDoKhauTru = @LyDoKhauTru, GhiChu = @GhiChu, UpdatedAt = GETDATE()
            WHERE HopDongId = @HopDongId";

        /// <summary>
        /// Lấy danh sách hợp đồng với đầy đủ thông tin
        /// </summary>
        public async Task<IEnumerable<HopDong>> GetAllWithDetailsAsync(string? trangThai = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, k.CCCD, k.Phone
                FROM HOPDONG hd
                JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE (@TrangThai IS NULL OR hd.TrangThai = @TrangThai)
                ORDER BY hd.NgayBatDau DESC";
            return await conn.QueryAsync<HopDong>(sql, new { TrangThai = trangThai });
        }

        /// <summary>
        /// Lấy hợp đồng active của phòng
        /// </summary>
        public async Task<HopDong?> GetActiveByPhongIdAsync(int phongId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, k.CCCD, k.Phone
                FROM HOPDONG hd
                JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE hd.PhongId = @PhongId AND hd.TrangThai = N'Active'";
            return await conn.QueryFirstOrDefaultAsync<HopDong>(sql, new { PhongId = phongId });
        }

        /// <summary>
        /// Lấy hợp đồng active của khách
        /// </summary>
        public async Task<HopDong?> GetActiveByKhachIdAsync(int khachId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue
                FROM HOPDONG hd
                JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE hd.KhachId = @KhachId AND hd.TrangThai = N'Active'";
            return await conn.QueryFirstOrDefaultAsync<HopDong>(sql, new { KhachId = khachId });
        }

        /// <summary>
        /// Lấy danh sách hợp đồng sắp hết hạn
        /// </summary>
        public async Task<IEnumerable<HopDong>> GetExpiringSoonAsync(int days = 30)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, k.Phone
                FROM HOPDONG hd
                JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE hd.TrangThai = N'Active'
                  AND DATEDIFF(DAY, GETDATE(), hd.NgayKetThuc) BETWEEN 0 AND @Days
                ORDER BY hd.NgayKetThuc";
            return await conn.QueryAsync<HopDong>(sql, new { Days = days });
        }

        /// <summary>
        /// Tạo hợp đồng mới (bao gồm cập nhật trạng thái phòng)
        /// </summary>
        public async Task<int> CreateContractAsync(HopDong hopDong)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                // Insert hợp đồng
                var sql = GetInsertQuery();
                var hopDongId = await conn.ExecuteScalarAsync<int>(sql, hopDong, trans);

                // Cập nhật trạng thái phòng
                await conn.ExecuteAsync(
                    "UPDATE PHONGTRO SET TrangThai = N'Đang thuê', UpdatedAt = GETDATE() WHERE PhongId = @PhongId",
                    new { hopDong.PhongId }, trans);

                trans.Commit();
                return hopDongId;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Chấm dứt hợp đồng
        /// </summary>
        public async Task<bool> TerminateContractAsync(int hopDongId, decimal tienHoanCoc, decimal tienKhauTru, string? lyDoKhauTru)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                // Lấy thông tin hợp đồng
                var hopDong = await conn.QueryFirstAsync<HopDong>(
                    "SELECT * FROM HOPDONG WHERE HopDongId = @HopDongId",
                    new { HopDongId = hopDongId }, trans);

                // Cập nhật hợp đồng
                await conn.ExecuteAsync(@"
                    UPDATE HOPDONG SET
                        TrangThai = N'Terminated', NgayThanhLy = GETDATE(),
                        TienHoanCoc = @TienHoanCoc, TienKhauTru = @TienKhauTru,
                        LyDoKhauTru = @LyDoKhauTru, UpdatedAt = GETDATE()
                    WHERE HopDongId = @HopDongId",
                    new { HopDongId = hopDongId, TienHoanCoc = tienHoanCoc, TienKhauTru = tienKhauTru, LyDoKhauTru = lyDoKhauTru }, trans);

                // Cập nhật trạng thái phòng
                await conn.ExecuteAsync(
                    "UPDATE PHONGTRO SET TrangThai = N'Trống', UpdatedAt = GETDATE() WHERE PhongId = @PhongId",
                    new { hopDong.PhongId }, trans);

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Gia hạn hợp đồng
        /// </summary>
        public async Task<bool> ExtendContractAsync(int hopDongId, DateTime ngayKetThucMoi, decimal? giaThueMoi = null)
        {
            using var conn = GetConnection();
            var sql = @"
                UPDATE HOPDONG SET
                    NgayKetThuc = @NgayKetThuc,
                    GiaThue = ISNULL(@GiaThue, GiaThue),
                    UpdatedAt = GETDATE()
                WHERE HopDongId = @HopDongId";
            return await conn.ExecuteAsync(sql,
                new { HopDongId = hopDongId, NgayKetThuc = ngayKetThucMoi, GiaThue = giaThueMoi }) > 0;
        }

        /// <summary>
        /// Sinh mã hợp đồng tự động
        /// </summary>
        public async Task<string> GenerateMaHopDongAsync()
        {
            using var conn = GetConnection();
            var prefix = $"HD{DateTime.Now:yyyyMM}";
            var sql = @"
                SELECT TOP 1 MaHopDong FROM HOPDONG
                WHERE MaHopDong LIKE @Prefix + '%'
                ORDER BY MaHopDong DESC";
            var lastCode = await conn.QueryFirstOrDefaultAsync<string>(sql, new { Prefix = prefix });

            if (string.IsNullOrEmpty(lastCode))
                return $"{prefix}001";

            var num = int.Parse(lastCode.Substring(8)) + 1;
            return $"{prefix}{num:D3}";
        }

        /// <summary>
        /// Lấy hợp đồng active theo UserId (cho Tenant)
        /// </summary>
        public async Task<HopDong?> GetActiveByUserIdAsync(int userId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue
                FROM HOPDONG hd
                JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE k.UserId = @UserId AND hd.TrangThai = N'Active'";
            return await conn.QueryFirstOrDefaultAsync<HopDong>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Lấy tất cả hợp đồng theo UserId (cho Tenant xem lịch sử)
        /// </summary>
        public async Task<IEnumerable<HopDong>> GetByTenantUserIdAsync(int userId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue
                FROM HOPDONG hd
                JOIN PHONGTRO p ON hd.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE k.UserId = @UserId
                ORDER BY hd.NgayBatDau DESC";
            return await conn.QueryAsync<HopDong>(sql, new { UserId = userId });
        }
    }
}
