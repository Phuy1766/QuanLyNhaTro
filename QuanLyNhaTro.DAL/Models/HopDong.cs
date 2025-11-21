namespace QuanLyNhaTro.DAL.Models
{
    public class HopDong
    {
        public int HopDongId { get; set; }
        public string MaHopDong { get; set; } = string.Empty;
        public int PhongId { get; set; }
        public int KhachId { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public decimal GiaThue { get; set; }
        public decimal TienCoc { get; set; }
        public int ChuKyThanhToan { get; set; } = 1;
        public string TrangThai { get; set; } = "Active";
        public DateTime? NgayThanhLy { get; set; }
        public decimal? TienHoanCoc { get; set; }
        public decimal? TienKhauTru { get; set; }
        public string? LyDoKhauTru { get; set; }
        public string? GhiChu { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? MaPhong { get; set; }
        public string? BuildingName { get; set; }
        public string? TenKhachThue { get; set; }
        public string? CCCD { get; set; }
        public string? Phone { get; set; }

        // Calculated
        public int SoNgayConLai => (NgayKetThuc - DateTime.Today).Days;
        public bool SapHetHan => SoNgayConLai <= 30 && SoNgayConLai > 0;
    }
}
