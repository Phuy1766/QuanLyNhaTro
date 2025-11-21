using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class PhongTroRepository : BaseRepository<PhongTro>
    {
        protected override string GetTableName() => "PHONGTRO";
        protected override string GetPrimaryKey() => "PhongId";

        protected override string GetInsertQuery() => @"
            INSERT INTO PHONGTRO (MaPhong, BuildingId, LoaiPhongId, Tang, DienTich, GiaThue, SoNguoiToiDa, TrangThai, MoTa, HinhAnh, IsActive)
            VALUES (@MaPhong, @BuildingId, @LoaiPhongId, @Tang, @DienTich, @GiaThue, @SoNguoiToiDa, @TrangThai, @MoTa, @HinhAnh, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE PHONGTRO SET
                MaPhong = @MaPhong, BuildingId = @BuildingId, LoaiPhongId = @LoaiPhongId,
                Tang = @Tang, DienTich = @DienTich, GiaThue = @GiaThue, SoNguoiToiDa = @SoNguoiToiDa,
                TrangThai = @TrangThai, MoTa = @MoTa, HinhAnh = @HinhAnh,
                IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE PhongId = @PhongId";

        /// <summary>
        /// Lấy danh sách phòng với đầy đủ thông tin
        /// </summary>
        public async Task<IEnumerable<PhongTro>> GetAllWithDetailsAsync(int? buildingId = null, string? trangThai = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT p.*, b.BuildingName, b.BuildingCode, lp.TenLoai,
                       hd.MaHopDong, hd.HopDongId, k.HoTen AS TenKhachThue
                FROM PHONGTRO p
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN LOAIPHONG lp ON p.LoaiPhongId = lp.LoaiPhongId
                LEFT JOIN HOPDONG hd ON p.PhongId = hd.PhongId AND hd.TrangThai = N'Active'
                LEFT JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
                WHERE p.IsActive = 1
                  AND (@BuildingId IS NULL OR p.BuildingId = @BuildingId)
                  AND (@TrangThai IS NULL OR p.TrangThai = @TrangThai)
                ORDER BY b.BuildingCode, p.Tang, p.MaPhong";

            return await conn.QueryAsync<PhongTro>(sql, new { BuildingId = buildingId, TrangThai = trangThai });
        }

        /// <summary>
        /// Lấy danh sách phòng trống
        /// </summary>
        public async Task<IEnumerable<PhongTro>> GetAvailableRoomsAsync(int? buildingId = null)
        {
            return await GetAllWithDetailsAsync(buildingId, "Trống");
        }

        /// <summary>
        /// Kiểm tra mã phòng đã tồn tại trong tòa nhà chưa
        /// </summary>
        public async Task<bool> MaPhongExistsAsync(string maPhong, int buildingId, int? excludeId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM PHONGTRO WHERE MaPhong = @MaPhong AND BuildingId = @BuildingId AND IsActive = 1";
            if (excludeId.HasValue)
                sql += " AND PhongId != @ExcludeId";

            return await conn.ExecuteScalarAsync<int>(sql,
                new { MaPhong = maPhong, BuildingId = buildingId, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Kiểm tra phòng có hợp đồng active không
        /// </summary>
        public async Task<bool> HasActiveContractAsync(int phongId)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM HOPDONG WHERE PhongId = @PhongId AND TrangThai = N'Active'";
            return await conn.ExecuteScalarAsync<int>(sql, new { PhongId = phongId }) > 0;
        }

        /// <summary>
        /// Cập nhật trạng thái phòng
        /// </summary>
        public async Task<bool> UpdateTrangThaiAsync(int phongId, string trangThai)
        {
            using var conn = GetConnection();
            var sql = "UPDATE PHONGTRO SET TrangThai = @TrangThai, UpdatedAt = GETDATE() WHERE PhongId = @PhongId";
            return await conn.ExecuteAsync(sql, new { PhongId = phongId, TrangThai = trangThai }) > 0;
        }

        /// <summary>
        /// Cập nhật giá thuê và lưu lịch sử
        /// </summary>
        public async Task<bool> UpdateGiaThueAsync(int phongId, decimal giaMoi, int userId, string? ghiChu = null)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                // Lấy giá cũ
                var giaCu = await conn.ExecuteScalarAsync<decimal?>(
                    "SELECT GiaThue FROM PHONGTRO WHERE PhongId = @PhongId",
                    new { PhongId = phongId }, trans);

                // Cập nhật ngày kết thúc cho lịch sử giá cũ
                await conn.ExecuteAsync(@"
                    UPDATE LICHSU_GIA SET NgayKetThuc = GETDATE()
                    WHERE PhongId = @PhongId AND NgayKetThuc IS NULL",
                    new { PhongId = phongId }, trans);

                // Thêm lịch sử giá mới
                await conn.ExecuteAsync(@"
                    INSERT INTO LICHSU_GIA (PhongId, GiaCu, GiaMoi, NgayApDung, GhiChu, CreatedBy)
                    VALUES (@PhongId, @GiaCu, @GiaMoi, GETDATE(), @GhiChu, @CreatedBy)",
                    new { PhongId = phongId, GiaCu = giaCu, GiaMoi = giaMoi, GhiChu = ghiChu, CreatedBy = userId }, trans);

                // Cập nhật giá mới cho phòng
                await conn.ExecuteAsync(
                    "UPDATE PHONGTRO SET GiaThue = @GiaThue, UpdatedAt = GETDATE() WHERE PhongId = @PhongId",
                    new { PhongId = phongId, GiaThue = giaMoi }, trans);

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
        /// Lấy lịch sử giá của phòng
        /// </summary>
        public async Task<IEnumerable<LichSuGia>> GetLichSuGiaAsync(int phongId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM LICHSU_GIA WHERE PhongId = @PhongId ORDER BY NgayApDung DESC";
            return await conn.QueryAsync<LichSuGia>(sql, new { PhongId = phongId });
        }

        /// <summary>
        /// Lấy danh sách loại phòng
        /// </summary>
        public async Task<IEnumerable<LoaiPhong>> GetLoaiPhongAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<LoaiPhong>("SELECT * FROM LOAIPHONG ORDER BY TenLoai");
        }

        /// <summary>
        /// Lấy phòng theo mã phòng (cho Tenant)
        /// </summary>
        public async Task<PhongTro?> GetByMaPhongAsync(string maPhong)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT p.*, b.BuildingName, lp.TenLoai
                FROM PHONGTRO p
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN LOAIPHONG lp ON p.LoaiPhongId = lp.LoaiPhongId
                WHERE p.MaPhong = @MaPhong AND p.IsActive = 1";
            return await conn.QueryFirstOrDefaultAsync<PhongTro>(sql, new { MaPhong = maPhong });
        }
    }
}
