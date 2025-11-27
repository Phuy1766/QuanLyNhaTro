namespace QuanLyNhaTro.DAL.Models
{
    public class HoaDon
    {
        public int HoaDonId { get; set; }
        public string MaHoaDon { get; set; } = string.Empty;
        public int HopDongId { get; set; }
        public DateTime ThangNam { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TongTienDichVu { get; set; }
        public decimal TongCong { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal ConNo { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        public string TrangThai { get; set; } = "ChuaThanhToan";
        public string? GhiChu { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public string? MaPhong { get; set; }
        public string? BuildingName { get; set; }
        public string? TenKhachThue { get; set; }
        public string? MaHopDong { get; set; }
        public string? Email { get; set; }

        // Chi tiết dịch vụ
        public List<ChiTietHoaDon> ChiTietDichVu { get; set; } = new();

        // Calculated
        public bool QuaHan => TrangThai != "DaThanhToan" && NgayHetHan.HasValue && NgayHetHan.Value < DateTime.Today;

        // For Tenant invoice detail
        public decimal GiaPhong { get; set; }
        public decimal TienDien { get; set; }
        public decimal TienNuoc { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal SoDienCu { get; set; }
        public decimal SoDienMoi { get; set; }
        public decimal SoNuocCu { get; set; }
        public decimal SoNuocMoi { get; set; }
    }

    public class ChiTietHoaDon
    {
        public int ChiTietId { get; set; }
        public int HoaDonId { get; set; }
        public int DichVuId { get; set; }
        public decimal? ChiSoCu { get; set; }
        public decimal? ChiSoMoi { get; set; }
        public decimal? SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string? GhiChu { get; set; }

        // Navigation
        public string? TenDichVu { get; set; }
        public string? DonViTinh { get; set; }
    }
}
