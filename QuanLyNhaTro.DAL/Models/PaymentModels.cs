namespace QuanLyNhaTro.DAL.Models
{
    /// <summary>
    /// Model cấu hình thanh toán
    /// </summary>
    public class PaymentConfig
    {
        public int ConfigId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public string TransferTemplate { get; set; } = "NTPRO_{MaYeuCau}_{MaPhong}";
        public int DepositMonths { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Model phiếu thanh toán cọc
    /// </summary>
    public class BookingPayment
    {
        public int MaThanhToan { get; set; }
        public int MaYeuCau { get; set; }
        public decimal SoTien { get; set; }
        public string NoiDungChuyenKhoan { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "Pending"; // Pending, WaitingConfirm, Paid, Canceled, Refunded
        public string KieuThanhToan { get; set; } = "QRBank"; // QRBank, MoMo, ZaloPay, Cash
        public int? BankConfigId { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public int? NguoiXacNhan { get; set; }
        public string? GhiChu { get; set; }

        // Navigation/Display properties
        public string? MaPhong { get; set; }
        public string? TenToaNha { get; set; }
        public string? TenTenant { get; set; }
        public string? SdtTenant { get; set; }
        public decimal? GiaPhong { get; set; }
        public string? BankName { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public string? TenNguoiXacNhan { get; set; }
    }

    /// <summary>
    /// DTO cho kết quả tạo yêu cầu thuê + thanh toán
    /// </summary>
    public class CreateBookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int MaYeuCau { get; set; }
        public int MaThanhToan { get; set; }
    }

    /// <summary>
    /// DTO hiển thị yêu cầu thuê phòng với thông tin thanh toán
    /// </summary>
    public class BookingRequestDTO
    {
        public int MaYeuCau { get; set; }
        public int PhongId { get; set; }
        public string? MaPhong { get; set; }
        public string? TenToaNha { get; set; }
        public int MaTenant { get; set; }
        public string? TenTenant { get; set; }
        public string? SdtTenant { get; set; }
        public DateTime NgayGui { get; set; }
        public DateTime NgayBatDauMongMuon { get; set; }
        public int SoNguoi { get; set; }
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime? NgayXuLy { get; set; }
        public string? TenNguoiXuLy { get; set; }
        public string? LyDoTuChoi { get; set; }
        public decimal? GiaPhong { get; set; }
        public int? SoNguoiToiDa { get; set; }
        public decimal? DienTich { get; set; }

        // Payment info
        public int? MaThanhToan { get; set; }
        public decimal? SoTienCoc { get; set; }
        public string? TrangThaiThanhToan { get; set; }
        public DateTime? NgayThanhToan { get; set; }

        // Display helpers
        public string TrangThaiDisplay => TrangThai switch
        {
            "PendingPayment" => "Chờ thanh toán",
            "WaitingConfirm" => "Chờ xác nhận TT",
            "PendingApprove" => "Chờ duyệt HĐ",
            "Approved" => "Đã duyệt",
            "Rejected" => "Từ chối",
            "Canceled" => "Đã hủy",
            _ => TrangThai
        };

        public string TrangThaiThanhToanDisplay => TrangThaiThanhToan switch
        {
            "Pending" => "Chưa thanh toán",
            "WaitingConfirm" => "Chờ xác nhận",
            "Paid" => "Đã thanh toán",
            "Canceled" => "Đã hủy",
            "Refunded" => "Đã hoàn tiền",
            _ => TrangThaiThanhToan ?? "N/A"
        };
    }
}
