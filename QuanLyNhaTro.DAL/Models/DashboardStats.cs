namespace QuanLyNhaTro.DAL.Models
{
    /// <summary>
    /// Model chứa thống kê cho Dashboard
    /// </summary>
    public class DashboardStats
    {
        public int TongPhong { get; set; }
        public int PhongTrong { get; set; }
        public int PhongDangThue { get; set; }
        public int PhongDangSua { get; set; }
        public int TongKhachThue { get; set; }
        public decimal DoanhThuThang { get; set; }
        public decimal TongCongNo { get; set; }
        public int HopDongSapHetHan { get; set; }
        public int HoaDonQuaHan { get; set; }
        public int BaoTriMoi { get; set; }

        public List<DoanhThuTheoThang> DoanhThu12Thang { get; set; } = new();
        public List<PhongTheoTrangThai> PhongTheoTrangThai { get; set; } = new();
    }

    public class DoanhThuTheoThang
    {
        public string Thang { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
    }

    public class PhongTheoTrangThai
    {
        public string TrangThai { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }
}
