using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class DashboardRepository
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        /// <summary>
        /// Lấy thống kê tổng quan cho Dashboard
        /// </summary>
        public async Task<DashboardStats> GetStatsAsync()
        {
            using var conn = GetConnection();
            var stats = new DashboardStats();

            // Số phòng theo trạng thái
            var roomStats = await conn.QueryFirstAsync<dynamic>(@"
                SELECT
                    COUNT(*) AS TongPhong,
                    SUM(CASE WHEN TrangThai = N'Trống' THEN 1 ELSE 0 END) AS PhongTrong,
                    SUM(CASE WHEN TrangThai = N'Đang thuê' THEN 1 ELSE 0 END) AS PhongDangThue,
                    SUM(CASE WHEN TrangThai = N'Đang sửa' THEN 1 ELSE 0 END) AS PhongDangSua
                FROM PHONGTRO WHERE IsActive = 1");

            stats.TongPhong = (int)roomStats.TongPhong;
            stats.PhongTrong = (int)roomStats.PhongTrong;
            stats.PhongDangThue = (int)roomStats.PhongDangThue;
            stats.PhongDangSua = (int)roomStats.PhongDangSua;

            // Số khách thuê đang hoạt động
            stats.TongKhachThue = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(DISTINCT KhachId) FROM HOPDONG WHERE TrangThai = N'Active'");

            // Doanh thu tháng này (tổng đã thanh toán của các hóa đơn tháng này)
            stats.DoanhThuThang = await conn.ExecuteScalarAsync<decimal>(@"
                SELECT ISNULL(SUM(DaThanhToan), 0)
                FROM HOADON
                WHERE MONTH(ThangNam) = MONTH(GETDATE())
                  AND YEAR(ThangNam) = YEAR(GETDATE())");

            // Tổng công nợ
            stats.TongCongNo = await conn.ExecuteScalarAsync<decimal>(
                "SELECT ISNULL(SUM(ConNo), 0) FROM HOADON WHERE TrangThai != N'DaThanhToan'");

            // Hợp đồng sắp hết hạn (30 ngày)
            stats.HopDongSapHetHan = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM HOPDONG
                WHERE TrangThai = N'Active'
                  AND DATEDIFF(DAY, GETDATE(), NgayKetThuc) BETWEEN 0 AND 30");

            // Hóa đơn quá hạn
            stats.HoaDonQuaHan = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM HOADON
                WHERE TrangThai = N'ChuaThanhToan' AND NgayHetHan < GETDATE()");

            // Bảo trì mới
            stats.BaoTriMoi = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM BAOTRI_TICKET WHERE TrangThai = N'Mới'");

            // Doanh thu 6 tháng gần nhất (bao gồm tháng hiện tại)
            stats.DoanhThu12Thang = (await conn.QueryAsync<DoanhThuTheoThang>(@"
                SELECT
                    FORMAT(ThangNam, 'MM/yy') AS Thang,
                    ISNULL(SUM(DaThanhToan), 0) AS DoanhThu
                FROM HOADON
                WHERE ThangNam >= DATEADD(MONTH, -5, DATEADD(DAY, 1-DAY(GETDATE()), GETDATE()))
                  AND ThangNam <= DATEADD(DAY, 1-DAY(GETDATE()), GETDATE())
                GROUP BY FORMAT(ThangNam, 'MM/yy'), YEAR(ThangNam), MONTH(ThangNam)
                ORDER BY YEAR(ThangNam), MONTH(ThangNam)")).ToList();

            // Phòng theo trạng thái (cho biểu đồ tròn)
            stats.PhongTheoTrangThai = new List<PhongTheoTrangThai>
            {
                new() { TrangThai = "Trống", SoLuong = stats.PhongTrong },
                new() { TrangThai = "Đang thuê", SoLuong = stats.PhongDangThue },
                new() { TrangThai = "Đang sửa", SoLuong = stats.PhongDangSua }
            };

            return stats;
        }

        /// <summary>
        /// Lấy doanh thu theo tháng trong năm
        /// </summary>
        public async Task<IEnumerable<DoanhThuTheoThang>> GetRevenueByYearAsync(int year)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT
                    FORMAT(ThangNam, 'MM/yyyy') AS Thang,
                    SUM(DaThanhToan) AS DoanhThu
                FROM HOADON
                WHERE YEAR(ThangNam) = @Year
                GROUP BY FORMAT(ThangNam, 'MM/yyyy'), MONTH(ThangNam)
                ORDER BY MONTH(ThangNam)";
            return await conn.QueryAsync<DoanhThuTheoThang>(sql, new { Year = year });
        }

        /// <summary>
        /// Lấy thống kê phòng theo tòa nhà
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetRoomStatsByBuildingAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT
                    b.BuildingName,
                    COUNT(*) AS TongPhong,
                    SUM(CASE WHEN p.TrangThai = N'Trống' THEN 1 ELSE 0 END) AS PhongTrong,
                    SUM(CASE WHEN p.TrangThai = N'Đang thuê' THEN 1 ELSE 0 END) AS PhongDangThue
                FROM BUILDING b
                LEFT JOIN PHONGTRO p ON b.BuildingId = p.BuildingId AND p.IsActive = 1
                WHERE b.IsActive = 1
                GROUP BY b.BuildingId, b.BuildingName
                ORDER BY b.BuildingName";
            return await conn.QueryAsync(sql);
        }
    }
}
