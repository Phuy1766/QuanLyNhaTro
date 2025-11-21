namespace QuanLyNhaTro.DAL.Models
{
    public class PhongTro
    {
        public int PhongId { get; set; }
        public string MaPhong { get; set; } = string.Empty;
        public int BuildingId { get; set; }
        public int? LoaiPhongId { get; set; }
        public int Tang { get; set; } = 1;
        public decimal? DienTich { get; set; }
        public decimal GiaThue { get; set; }
        public int SoNguoiToiDa { get; set; } = 2;
        public string TrangThai { get; set; } = "Trống";
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? BuildingName { get; set; }
        public string? BuildingCode { get; set; }
        public string? TenLoai { get; set; }
        public string? MaHopDong { get; set; }
        public int? HopDongId { get; set; }
        public string? TenKhachThue { get; set; }
    }

    public class LoaiPhong
    {
        public int LoaiPhongId { get; set; }
        public string TenLoai { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LichSuGia
    {
        public int LichSuGiaId { get; set; }
        public int PhongId { get; set; }
        public decimal? GiaCu { get; set; }
        public decimal GiaMoi { get; set; }
        public DateTime NgayApDung { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string? GhiChu { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
