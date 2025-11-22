using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class HoaDonRepository : BaseRepository<HoaDon>
    {
        protected override string GetTableName() => "HOADON";
        protected override string GetPrimaryKey() => "HoaDonId";

        protected override string GetInsertQuery() => @"
            INSERT INTO HOADON (MaHoaDon, HopDongId, ThangNam, TienPhong, TongTienDichVu, TongCong, DaThanhToan, ConNo, NgayHetHan, TrangThai, GhiChu, CreatedBy)
            VALUES (@MaHoaDon, @HopDongId, @ThangNam, @TienPhong, @TongTienDichVu, @TongCong, @DaThanhToan, @ConNo, @NgayHetHan, @TrangThai, @GhiChu, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE HOADON SET
                TongTienDichVu = @TongTienDichVu, TongCong = @TongCong, DaThanhToan = @DaThanhToan,
                ConNo = @ConNo, NgayThanhToan = @NgayThanhToan, TrangThai = @TrangThai,
                GhiChu = @GhiChu, UpdatedAt = GETDATE()
            WHERE HoaDonId = @HoaDonId";

        /// <summary>
        /// Lấy danh sách hóa đơn với đầy đủ thông tin
        /// </summary>
        public async Task<IEnumerable<HoaDon>> GetAllWithDetailsAsync(string? trangThai = null, int? year = null, int? month = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, k.Email, hp.MaHopDong
                FROM HOADON hd
                JOIN HOPDONG hp ON hd.HopDongId = hp.HopDongId
                JOIN PHONGTRO p ON hp.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hp.KhachId = k.KhachId
                WHERE (@TrangThai IS NULL OR hd.TrangThai = @TrangThai)
                  AND (@Year IS NULL OR YEAR(hd.ThangNam) = @Year)
                  AND (@Month IS NULL OR MONTH(hd.ThangNam) = @Month)
                ORDER BY hd.ThangNam DESC, p.MaPhong";
            return await conn.QueryAsync<HoaDon>(sql, new { TrangThai = trangThai, Year = year, Month = month });
        }

        /// <summary>
        /// Lấy hóa đơn với chi tiết dịch vụ
        /// </summary>
        public async Task<HoaDon?> GetWithDetailsAsync(int hoaDonId)
        {
            using var conn = GetConnection();

            // Lấy thông tin hóa đơn
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, k.Email, hp.MaHopDong
                FROM HOADON hd
                JOIN HOPDONG hp ON hd.HopDongId = hp.HopDongId
                JOIN PHONGTRO p ON hp.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hp.KhachId = k.KhachId
                WHERE hd.HoaDonId = @HoaDonId";
            var hoaDon = await conn.QueryFirstOrDefaultAsync<HoaDon>(sql, new { HoaDonId = hoaDonId });

            if (hoaDon != null)
            {
                // Lấy chi tiết dịch vụ
                var sqlChiTiet = @"
                    SELECT ct.*, dv.TenDichVu, dv.DonViTinh
                    FROM CHITIETHOADON ct
                    JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                    WHERE ct.HoaDonId = @HoaDonId";
                hoaDon.ChiTietDichVu = (await conn.QueryAsync<ChiTietHoaDon>(sqlChiTiet, new { HoaDonId = hoaDonId })).ToList();
            }

            return hoaDon;
        }

        /// <summary>
        /// Lấy hóa đơn chưa thanh toán
        /// </summary>
        public async Task<IEnumerable<HoaDon>> GetUnpaidAsync()
        {
            return await GetAllWithDetailsAsync("ChuaThanhToan");
        }

        /// <summary>
        /// Lấy hóa đơn quá hạn
        /// </summary>
        public async Task<IEnumerable<HoaDon>> GetOverdueAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, k.Email
                FROM HOADON hd
                JOIN HOPDONG hp ON hd.HopDongId = hp.HopDongId
                JOIN PHONGTRO p ON hp.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                JOIN KHACHTHUE k ON hp.KhachId = k.KhachId
                WHERE hd.TrangThai = N'ChuaThanhToan' AND hd.NgayHetHan < GETDATE()
                ORDER BY hd.NgayHetHan";
            return await conn.QueryAsync<HoaDon>(sql);
        }

        /// <summary>
        /// Lấy hóa đơn theo hợp đồng
        /// </summary>
        public async Task<IEnumerable<HoaDon>> GetByHopDongAsync(int hopDongId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM HOADON WHERE HopDongId = @HopDongId ORDER BY ThangNam DESC";
            return await conn.QueryAsync<HoaDon>(sql, new { HopDongId = hopDongId });
        }

        /// <summary>
        /// Tạo hóa đơn mới với chi tiết dịch vụ
        /// </summary>
        public async Task<int> CreateWithDetailsAsync(HoaDon hoaDon, List<ChiTietHoaDon> chiTietList)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                // Insert hóa đơn
                var hoaDonId = await conn.ExecuteScalarAsync<int>(GetInsertQuery(), hoaDon, trans);

                // Insert chi tiết
                foreach (var ct in chiTietList)
                {
                    ct.HoaDonId = hoaDonId;
                    await conn.ExecuteAsync(@"
                        INSERT INTO CHITIETHOADON (HoaDonId, DichVuId, ChiSoCu, ChiSoMoi, SoLuong, DonGia, ThanhTien, GhiChu)
                        VALUES (@HoaDonId, @DichVuId, @ChiSoCu, @ChiSoMoi, @SoLuong, @DonGia, @ThanhTien, @GhiChu)",
                        ct, trans);
                }

                trans.Commit();
                return hoaDonId;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Thanh toán hóa đơn
        /// </summary>
        public async Task<bool> PaymentAsync(int hoaDonId, decimal soTienThanhToan)
        {
            using var conn = GetConnection();
            var sql = @"
                UPDATE HOADON SET
                    DaThanhToan = DaThanhToan + @SoTien,
                    ConNo = TongCong - DaThanhToan - @SoTien,
                    TrangThai = CASE WHEN TongCong <= DaThanhToan + @SoTien THEN N'DaThanhToan' ELSE TrangThai END,
                    NgayThanhToan = CASE WHEN TongCong <= DaThanhToan + @SoTien THEN GETDATE() ELSE NgayThanhToan END,
                    UpdatedAt = GETDATE()
                WHERE HoaDonId = @HoaDonId";
            return await conn.ExecuteAsync(sql, new { HoaDonId = hoaDonId, SoTien = soTienThanhToan }) > 0;
        }

        /// <summary>
        /// Tính tổng công nợ của khách
        /// </summary>
        public async Task<decimal> GetTotalDebtByKhachAsync(int khachId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT ISNULL(SUM(hd.ConNo), 0)
                FROM HOADON hd
                JOIN HOPDONG hp ON hd.HopDongId = hp.HopDongId
                WHERE hp.KhachId = @KhachId AND hd.TrangThai != N'DaThanhToan'";
            return await conn.ExecuteScalarAsync<decimal>(sql, new { KhachId = khachId });
        }

        /// <summary>
        /// Sinh mã hóa đơn tự động
        /// </summary>
        public async Task<string> GenerateMaHoaDonAsync(DateTime thangNam)
        {
            using var conn = GetConnection();
            var prefix = $"HD{thangNam:yyyyMM}";
            var sql = @"
                SELECT TOP 1 MaHoaDon FROM HOADON
                WHERE MaHoaDon LIKE @Prefix + '%'
                ORDER BY MaHoaDon DESC";
            var lastCode = await conn.QueryFirstOrDefaultAsync<string>(sql, new { Prefix = prefix });

            if (string.IsNullOrEmpty(lastCode))
                return $"{prefix}001";

            var num = int.Parse(lastCode.Substring(8)) + 1;
            return $"{prefix}{num:D3}";
        }

        /// <summary>
        /// Kiểm tra hóa đơn tháng đã tồn tại chưa
        /// </summary>
        public async Task<bool> ExistsForMonthAsync(int hopDongId, DateTime thangNam)
        {
            using var conn = GetConnection();
            var sql = @"SELECT COUNT(1) FROM HOADON
                        WHERE HopDongId = @HopDongId
                          AND YEAR(ThangNam) = YEAR(@ThangNam)
                          AND MONTH(ThangNam) = MONTH(@ThangNam)";
            return await conn.ExecuteScalarAsync<int>(sql, new { HopDongId = hopDongId, ThangNam = thangNam }) > 0;
        }

        /// <summary>
        /// Lấy hóa đơn theo mã hợp đồng (cho Tenant)
        /// </summary>
        public async Task<IEnumerable<HoaDon>> GetByContractAsync(string maHopDong)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT hd.*, p.MaPhong, k.HoTen AS TenKhachThue, hp.MaHopDong,
                       hp.GiaThue AS GiaPhong,
                       (SELECT ISNULL(SUM(ct.ThanhTien), 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu LIKE N'%Điện%') AS TienDien,
                       (SELECT ISNULL(SUM(ct.ThanhTien), 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu LIKE N'%Nước%') AS TienNuoc,
                       (SELECT ISNULL(SUM(ct.ThanhTien), 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu NOT LIKE N'%Điện%' AND dv.TenDichVu NOT LIKE N'%Nước%') AS TienDichVu,
                       (SELECT ISNULL(ChiSoCu, 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu LIKE N'%Điện%') AS SoDienCu,
                       (SELECT ISNULL(ChiSoMoi, 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu LIKE N'%Điện%') AS SoDienMoi,
                       (SELECT ISNULL(ChiSoCu, 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu LIKE N'%Nước%') AS SoNuocCu,
                       (SELECT ISNULL(ChiSoMoi, 0) FROM CHITIETHOADON ct
                        JOIN DICHVU dv ON ct.DichVuId = dv.DichVuId
                        WHERE ct.HoaDonId = hd.HoaDonId AND dv.TenDichVu LIKE N'%Nước%') AS SoNuocMoi
                FROM HOADON hd
                JOIN HOPDONG hp ON hd.HopDongId = hp.HopDongId
                JOIN PHONGTRO p ON hp.PhongId = p.PhongId
                JOIN KHACHTHUE k ON hp.KhachId = k.KhachId
                WHERE hp.MaHopDong = @MaHopDong
                ORDER BY hd.ThangNam DESC";
            return await conn.QueryAsync<HoaDon>(sql, new { MaHopDong = maHopDong });
        }

        /// <summary>
        /// Đánh dấu hóa đơn chờ xác nhận thanh toán (tenant đã chuyển khoản)
        /// </summary>
        public async Task<bool> MarkAsPendingPaymentConfirmationAsync(int hoaDonId, int tenantUserId)
        {
            using var conn = GetConnection();
            var sql = @"
                UPDATE HOADON SET
                    TrangThai = N'ChoXacNhan',
                    GhiChu = CONCAT(ISNULL(GhiChu, ''), CHAR(13) + CHAR(10),
                                   'Tenant xác nhận đã thanh toán lúc: ', FORMAT(GETDATE(), 'dd/MM/yyyy HH:mm')),
                    UpdatedAt = GETDATE()
                WHERE HoaDonId = @HoaDonId";
            return await conn.ExecuteAsync(sql, new { HoaDonId = hoaDonId }) > 0;
        }

        /// <summary>
        /// Admin xác nhận thanh toán hóa đơn
        /// </summary>
        public async Task<bool> AdminConfirmInvoicePaymentAsync(int hoaDonId, int adminUserId, bool isConfirmed, decimal? soTienThanhToan = null)
        {
            using var conn = GetConnection();

            if (isConfirmed)
            {
                // Lấy thông tin hóa đơn
                var hoaDon = await conn.QueryFirstOrDefaultAsync<HoaDon>(
                    "SELECT * FROM HOADON WHERE HoaDonId = @HoaDonId",
                    new { HoaDonId = hoaDonId });

                if (hoaDon == null)
                    return false;

                var soTien = soTienThanhToan ?? hoaDon.ConNo;

                var sql = @"
                    UPDATE HOADON SET
                        DaThanhToan = DaThanhToan + @SoTien,
                        ConNo = TongCong - DaThanhToan - @SoTien,
                        TrangThai = CASE WHEN TongCong <= DaThanhToan + @SoTien THEN N'DaThanhToan' ELSE N'ChuaThanhToan' END,
                        NgayThanhToan = CASE WHEN TongCong <= DaThanhToan + @SoTien THEN GETDATE() ELSE NgayThanhToan END,
                        GhiChu = CONCAT(ISNULL(GhiChu, ''), CHAR(13) + CHAR(10),
                                       'Admin xác nhận thanh toán ', FORMAT(@SoTien, 'N0'), ' VNĐ lúc: ', FORMAT(GETDATE(), 'dd/MM/yyyy HH:mm')),
                        UpdatedAt = GETDATE()
                    WHERE HoaDonId = @HoaDonId";
                return await conn.ExecuteAsync(sql, new { HoaDonId = hoaDonId, SoTien = soTien }) > 0;
            }
            else
            {
                // Từ chối thanh toán - đưa về trạng thái cũ
                var sql = @"
                    UPDATE HOADON SET
                        TrangThai = CASE WHEN ConNo > 0 THEN N'ChuaThanhToan' ELSE N'DaThanhToan' END,
                        GhiChu = CONCAT(ISNULL(GhiChu, ''), CHAR(13) + CHAR(10),
                                       'Admin từ chối xác nhận thanh toán lúc: ', FORMAT(GETDATE(), 'dd/MM/yyyy HH:mm')),
                        UpdatedAt = GETDATE()
                    WHERE HoaDonId = @HoaDonId";
                return await conn.ExecuteAsync(sql, new { HoaDonId = hoaDonId }) > 0;
            }
        }

        /// <summary>
        /// Lấy danh sách hóa đơn chờ xác nhận thanh toán
        /// </summary>
        public async Task<IEnumerable<HoaDon>> GetPendingPaymentConfirmationAsync()
        {
            return await GetAllWithDetailsAsync("ChoXacNhan");
        }
    }
}
