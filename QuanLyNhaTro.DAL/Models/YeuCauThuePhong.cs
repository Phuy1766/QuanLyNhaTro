namespace QuanLyNhaTro.DAL.Models
{
    /// <summary>
    /// Model Yêu cầu thuê phòng (Booking Request)
    /// </summary>
    public class YeuCauThuePhong
    {
        public int MaYeuCau { get; set; }
        public int PhongId { get; set; }
        public string? MaPhong { get; set; } // Display property from join
        public int MaTenant { get; set; }
        public DateTime NgayGui { get; set; }
        public DateTime NgayBatDauMongMuon { get; set; }
        public int SoNguoi { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayHetHan { get; set; }  // FIX 5.1: Hạn thanh toán
        public string TrangThai { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime? NgayXuLy { get; set; }
        public int? NguoiXuLy { get; set; }
        public string? LyDoTuChoi { get; set; }
        public string? MoTaHuyBoSung { get; set; }  // FIX 5.1: Lý do hủy tự động

        // Navigation/Display properties
        public string? TenTenant { get; set; }
        public string? SdtTenant { get; set; }
        public string? TenToaNha { get; set; }
        public decimal? GiaPhong { get; set; }
        public int? SoNguoiToiDa { get; set; }
        public decimal? DienTich { get; set; }
        public string? TenNguoiXuLy { get; set; }
    }

    /// <summary>
    /// Model Thông báo
    /// </summary>
    public class ThongBao
    {
        public int MaThongBao { get; set; }
        public int UserId { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiThongBao { get; set; } = string.Empty; // HoaDon, HopDong, BaoTri, ThuePhong, HeThong
        public string? MaLienKet { get; set; }
        public bool DaDoc { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayDoc { get; set; }
    }

    /// <summary>
    /// Model Tin nhắn hỗ trợ
    /// </summary>
    public class TinNhan
    {
        public int MaTinNhan { get; set; }
        public int NguoiGui { get; set; }
        public int NguoiNhan { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public bool DaDoc { get; set; }

        // Display properties
        public string? TenNguoiGui { get; set; }
        public string? TenNguoiNhan { get; set; }
    }

    /// <summary>
    /// DTO cho phòng trống hiển thị cho Tenant
    /// </summary>
    public class PhongTrongDTO
    {
        public int PhongId { get; set; }
        public string MaPhong { get; set; } = string.Empty;
        public string? MaToaNha { get; set; }
        public string? TenToaNha { get; set; }
        public int? Tang { get; set; }
        public decimal DienTich { get; set; }
        public decimal GiaThue { get; set; }
        public int SoNguoiToiDa { get; set; }
        public string? LoaiPhong { get; set; }
        public string? MoTa { get; set; }
        public string? TienNghi { get; set; }
        public bool HasPendingRequest { get; set; } // Đã có yêu cầu pending chưa
    }
}
