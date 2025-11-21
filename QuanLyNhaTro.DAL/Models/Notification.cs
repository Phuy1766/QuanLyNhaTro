namespace QuanLyNhaTro.DAL.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int? UserId { get; set; }
        public string? LoaiThongBao { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? NoiDung { get; set; }
        public string? DuongDan { get; set; }
        public bool DaDoc { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayDoc { get; set; }
    }

    public class ActivityLog
    {
        public int LogId { get; set; }
        public int? UserId { get; set; }
        public string? TenBang { get; set; }
        public string? MaBanGhi { get; set; }
        public string? HanhDong { get; set; }
        public string? DuLieuCu { get; set; }
        public string? DuLieuMoi { get; set; }
        public string? MoTa { get; set; }
        public string? IpAddress { get; set; }
        public DateTime NgayThucHien { get; set; }

        // Navigation
        public string? TenNguoiDung { get; set; }
    }

    public class CauHinh
    {
        public int CauHinhId { get; set; }
        public string MaCauHinh { get; set; } = string.Empty;
        public string TenCauHinh { get; set; } = string.Empty;
        public string? GiaTri { get; set; }
        public string LoaiDuLieu { get; set; } = "String";
        public string? MoTa { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
