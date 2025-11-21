namespace QuanLyNhaTro.DAL.Models
{
    public class DichVu
    {
        public int DichVuId { get; set; }
        public string MaDichVu { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string? DonViTinh { get; set; }
        public string LoaiDichVu { get; set; } = "CoDinh"; // CoDinh, TheoChiSo
        public string? MoTa { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
