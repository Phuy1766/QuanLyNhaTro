using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    /// <summary>
    /// Repository cho Yêu cầu thuê phòng (Booking Request)
    /// </summary>
    public class YeuCauThuePhongRepository
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        /// <summary>
        /// Lấy tất cả yêu cầu (cho Admin/Manager)
        /// </summary>
        public async Task<IEnumerable<YeuCauThuePhong>> GetAllAsync(string? trangThai = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT yc.*, p.MaPhong,
                       u.FullName AS TenTenant, u.Phone AS SdtTenant,
                       p.GiaThue AS GiaPhong, p.SoNguoiToiDa, p.DienTich,
                       b.BuildingName AS TenToaNha,
                       xu.FullName AS TenNguoiXuLy
                FROM YEUCAU_THUEPHONG yc
                INNER JOIN USERS u ON yc.MaTenant = u.UserId
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN USERS xu ON yc.NguoiXuLy = xu.UserId
                WHERE (@TrangThai IS NULL OR yc.TrangThai = @TrangThai)
                ORDER BY
                    CASE yc.TrangThai
                        WHEN 'Pending' THEN 1
                        WHEN 'Approved' THEN 2
                        ELSE 3
                    END,
                    yc.NgayGui DESC";
            return await conn.QueryAsync<YeuCauThuePhong>(sql, new { TrangThai = trangThai });
        }

        /// <summary>
        /// Lấy yêu cầu theo Tenant
        /// </summary>
        public async Task<IEnumerable<YeuCauThuePhong>> GetByTenantAsync(int tenantUserId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT yc.*, p.MaPhong,
                       p.GiaThue AS GiaPhong, p.SoNguoiToiDa, p.DienTich,
                       b.BuildingName AS TenToaNha
                FROM YEUCAU_THUEPHONG yc
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                WHERE yc.MaTenant = @TenantUserId
                ORDER BY yc.NgayGui DESC";
            return await conn.QueryAsync<YeuCauThuePhong>(sql, new { TenantUserId = tenantUserId });
        }

        /// <summary>
        /// Lấy yêu cầu theo ID
        /// </summary>
        public async Task<YeuCauThuePhong?> GetByIdAsync(int maYeuCau)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT yc.*, p.MaPhong,
                       u.FullName AS TenTenant, u.Phone AS SdtTenant,
                       p.GiaThue AS GiaPhong, p.SoNguoiToiDa, p.DienTich,
                       b.BuildingName AS TenToaNha
                FROM YEUCAU_THUEPHONG yc
                INNER JOIN USERS u ON yc.MaTenant = u.UserId
                INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                WHERE yc.MaYeuCau = @MaYeuCau";
            return await conn.QueryFirstOrDefaultAsync<YeuCauThuePhong>(sql, new { MaYeuCau = maYeuCau });
        }

        /// <summary>
        /// Tạo yêu cầu thuê phòng mới
        /// </summary>
        public async Task<int> CreateAsync(YeuCauThuePhong yeuCau)
        {
            using var conn = GetConnection();
            var sql = @"
                INSERT INTO YEUCAU_THUEPHONG (PhongId, MaTenant, NgayBatDauMongMuon, SoNguoi, GhiChu, TrangThai)
                VALUES (@PhongId, @MaTenant, @NgayBatDauMongMuon, @SoNguoi, @GhiChu, 'Pending');
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await conn.ExecuteScalarAsync<int>(sql, yeuCau);
        }

        /// <summary>
        /// Duyệt yêu cầu - Tạo hợp đồng
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveAsync(int maYeuCau, int nguoiXuLy, string maHopDong, DateTime ngayKetThuc, decimal tienCoc, string? ghiChu = null)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_ApproveBookingRequest",
                new { MaYeuCau = maYeuCau, NguoiXuLy = nguoiXuLy, MaHopDong = maHopDong, NgayKetThuc = ngayKetThuc, TienCoc = tienCoc, GhiChu = ghiChu },
                commandType: System.Data.CommandType.StoredProcedure);
            return (result.Success == 1, result.Message);
        }

        /// <summary>
        /// Từ chối yêu cầu
        /// </summary>
        public async Task<(bool Success, string Message)> RejectAsync(int maYeuCau, int nguoiXuLy, string lyDoTuChoi)
        {
            using var conn = GetConnection();
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_RejectBookingRequest",
                new { MaYeuCau = maYeuCau, NguoiXuLy = nguoiXuLy, LyDoTuChoi = lyDoTuChoi },
                commandType: System.Data.CommandType.StoredProcedure);
            return (result.Success == 1, result.Message);
        }

        /// <summary>
        /// Đếm yêu cầu pending
        /// </summary>
        public async Task<int> CountPendingAsync()
        {
            using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM YEUCAU_THUEPHONG WHERE TrangThai = 'Pending'");
        }

        /// <summary>
        /// Kiểm tra Tenant đã có yêu cầu pending cho phòng này chưa
        /// </summary>
        public async Task<bool> HasPendingRequestAsync(int tenantUserId, int phongId)
        {
            using var conn = GetConnection();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM YEUCAU_THUEPHONG WHERE MaTenant = @TenantUserId AND PhongId = @PhongId AND TrangThai = 'Pending'",
                new { TenantUserId = tenantUserId, PhongId = phongId });
            return count > 0;
        }

        /// <summary>
        /// Lấy danh sách phòng trống cho Tenant
        /// </summary>
        public async Task<IEnumerable<PhongTrongDTO>> GetAvailableRoomsAsync(int tenantUserId, string? buildingCode = null, decimal? giaMin = null, decimal? giaMax = null, int? soNguoi = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT p.PhongId, p.MaPhong, b.BuildingCode AS MaToaNha, b.BuildingName AS TenToaNha,
                       p.Tang, p.DienTich, p.GiaThue, p.SoNguoiToiDa,
                       lp.TenLoai AS LoaiPhong, p.MoTa,
                       CASE WHEN EXISTS (
                           SELECT 1 FROM YEUCAU_THUEPHONG yc
                           WHERE yc.PhongId = p.PhongId AND yc.MaTenant = @TenantUserId AND yc.TrangThai = 'Pending'
                       ) THEN 1 ELSE 0 END AS HasPendingRequest
                FROM PHONGTRO p
                LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN LOAIPHONG lp ON p.LoaiPhongId = lp.LoaiPhongId
                WHERE p.TrangThai = N'Trống'
                  AND (@BuildingCode IS NULL OR b.BuildingCode = @BuildingCode)
                  AND (@GiaMin IS NULL OR p.GiaThue >= @GiaMin)
                  AND (@GiaMax IS NULL OR p.GiaThue <= @GiaMax)
                  AND (@SoNguoi IS NULL OR p.SoNguoiToiDa >= @SoNguoi)
                ORDER BY b.BuildingName, p.MaPhong";
            return await conn.QueryAsync<PhongTrongDTO>(sql, new { TenantUserId = tenantUserId, BuildingCode = buildingCode, GiaMin = giaMin, GiaMax = giaMax, SoNguoi = soNguoi });
        }
    }
}
