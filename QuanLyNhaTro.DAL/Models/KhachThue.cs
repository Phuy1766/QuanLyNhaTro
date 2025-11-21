namespace QuanLyNhaTro.DAL.Models
{
    public class KhachThue
    {
        public int KhachId { get; set; }
        public string MaKhach { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string CCCD { get; set; } = string.Empty;
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? DiaChi { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? NgheNghiep { get; set; }
        public string? NoiLamViec { get; set; }
        public string? HinhAnh { get; set; }
        public int? UserId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? MaPhong { get; set; }
        public string? BuildingName { get; set; }
        public string? TrangThaiHopDong { get; set; }
    }
}
