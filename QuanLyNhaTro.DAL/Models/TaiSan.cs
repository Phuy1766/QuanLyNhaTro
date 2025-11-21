namespace QuanLyNhaTro.DAL.Models
{
    public class TaiSan
    {
        public int TaiSanId { get; set; }
        public string TenTaiSan { get; set; } = string.Empty;
        public string DonVi { get; set; } = "Cái";
        public decimal GiaTri { get; set; }
        public string? MoTa { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class TaiSanPhong
    {
        public int TaiSanPhongId { get; set; }
        public int PhongId { get; set; }
        public int TaiSanId { get; set; }
        public int SoLuong { get; set; } = 1;
        public string TinhTrang { get; set; } = "Tốt";
        public DateTime NgayNhap { get; set; }
        public string? GhiChu { get; set; }

        // Navigation
        public string? TenTaiSan { get; set; }
        public string? DonVi { get; set; }
        public decimal GiaTri { get; set; }
        public string? MaPhong { get; set; }
    }
}
