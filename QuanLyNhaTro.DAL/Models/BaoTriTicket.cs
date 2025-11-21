namespace QuanLyNhaTro.DAL.Models
{
    public class BaoTriTicket
    {
        public int TicketId { get; set; }
        public string MaTicket { get; set; } = string.Empty;
        public int PhongId { get; set; }
        public int? KhachId { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string MucDoUuTien { get; set; } = "Trung bình";
        public string TrangThai { get; set; } = "Mới";
        public DateTime NgayTao { get; set; }
        public DateTime? NgayXuLy { get; set; }
        public DateTime? NgayHoanThanh { get; set; }
        public int? NguoiXuLy { get; set; }
        public decimal ChiPhiSuaChua { get; set; }
        public string? KetQuaXuLy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public string? MaPhong { get; set; }
        public string? BuildingName { get; set; }
        public string? TenKhachThue { get; set; }
        public string? TenNguoiXuLy { get; set; }

        // For Tenant
        public string? LoaiSuCo { get; set; }
        public string? DoUuTien { get; set; }
        public string? GhiChu { get; set; }
    }
}
