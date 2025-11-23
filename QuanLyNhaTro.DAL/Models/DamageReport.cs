namespace QuanLyNhaTro.DAL.Models
{
    /// <summary>
    /// Model ghi nhận hư hỏng tài sản
    /// FIX Issue 4.1: Có chứng cứ (ảnh) + phê duyệt từ Admin
    /// </summary>
    public class DamageReport
    {
        public int DamageId { get; set; }
        public int HopDongId { get; set; }
        public int PhongId { get; set; }
        public int TaiSanId { get; set; }
        public string MoTa { get; set; } = string.Empty;
        public string MucDoHuHong { get; set; } = "Trung bình"; // Nhẹ, Trung bình, Nặng
        public decimal GiaTriHuHong { get; set; }
        public string? HinhAnhChungCu { get; set; }  // Path to image
        public DateTime NgayGhiNhan { get; set; }
        public int? NguoiGhiNhan { get; set; }
        public string TrangThai { get; set; } = "PendingApproval"; // PendingApproval, Approved, Rejected
        public DateTime? NgayPheDuyet { get; set; }
        public int? NguoiPheDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
        public string? GhiChu { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? MaPhong { get; set; }
        public string? TenTaiSan { get; set; }
        public string? TenNguoiGhiNhan { get; set; }
        public string? TenNguoiPheDuyet { get; set; }
    }
}
