using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    /// <summary>
    /// Service xử lý Thanh toán QR cọc
    /// FIX 5.2: Validate số tiền thanh toán = số tiền QR yêu cầu
    /// </summary>
    public class PaymentQRService
    {
        private readonly PaymentRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();
        private readonly NotificationRepository _notiRepo = new();

        /// <summary>
        /// Tenant báo đã thanh toán (xác nhận chuyển khoản)
        /// </summary>
        public async Task<(bool Success, string Message)> ConfirmPaymentByTenantAsync(int maThanhToan, int maTenant)
        {
            var payment = await _repo.GetPaymentByIdAsync(maThanhToan);
            if (payment == null)
                return (false, "Phiếu thanh toán không tồn tại!");

            if (payment.TrangThai != "Pending")
                return (false, $"Phiếu thanh toán không ở trạng thái chờ thanh toán (Hiện tại: {payment.TrangThai})!");

            var result = await _repo.ConfirmPaymentByTenantAsync(maThanhToan, maTenant);

            if (result.Success)
            {
                await _logRepo.LogAsync(maTenant, "BOOKING_PAYMENT", maThanhToan.ToString(), "UPDATE",
                    moTa: $"Tenant xác nhận đã chuyển khoản {payment.SoTien:N0} VND");
            }

            return result;
        }

        /// <summary>
        /// Admin xác nhận thanh toán
        /// 🔴 FIX 5.2: Validate số tiền thực tế = số tiền QR yêu cầu
        /// </summary>
        public async Task<(bool Success, string Message)> AdminConfirmPaymentAsync(
            int maThanhToan, int adminId, decimal soTienThucTe, string? ghiChu = null)
        {
            var payment = await _repo.GetPaymentByIdAsync(maThanhToan);
            if (payment == null)
                return (false, "Phiếu thanh toán không tồn tại!");

            if (payment.TrangThai != "WaitingConfirm")
                return (false, $"Phiếu thanh toán không ở trạng thái chờ xác nhận (Hiện tại: {payment.TrangThai})!");

            // 🔴 FIX 5.2: VALIDATION QUAN TRỌNG
            if (soTienThucTe <= 0)
                return (false, "Số tiền xác nhận phải lớn hơn 0!");

            if (soTienThucTe != payment.SoTien)
            {
                // Tính tolerance: cho phép sai lệch ≤ 1000 đ (vì dư ngoại tệ, ngân hàng, etc)
                if (Math.Abs(soTienThucTe - payment.SoTien) > 1000)
                {
                    return (false, $@"
                        ❌ SỐ TIỀN KHÔNG KHỚP!
                        Yêu cầu: {payment.SoTien:N0} VND
                        Thực tế: {soTienThucTe:N0} VND
                        Chênh lệch: {Math.Abs(soTienThucTe - payment.SoTien):N0} VND
                        
                        Vui lòng kiểm tra lại. Nếu Tenant thanh toán không đủ, hãy yêu cầu thanh toán thêm hoặc từ chối.
                    ");
                }
                else
                {
                    // Tạo ghi chú về sai lệch nhỏ
                    if (ghiChu == null)
                        ghiChu = $"Dư lệch {soTienThucTe - payment.SoTien:N0} VND (chấp nhận)";
                    else
                        ghiChu += $" [Dư lệch {soTienThucTe - payment.SoTien:N0} VND]";
                }
            }

            var result = await _repo.AdminConfirmPaymentAsync(maThanhToan, adminId, true, ghiChu);

            if (result.Success)
            {
                await _logRepo.LogAsync(adminId, "BOOKING_PAYMENT", maThanhToan.ToString(), "UPDATE",
                    moTa: $"Admin xác nhận thanh toán {soTienThucTe:N0} VND");
            }

            return result;
        }

        /// <summary>
        /// Admin từ chối thanh toán
        /// </summary>
        public async Task<(bool Success, string Message)> AdminRejectPaymentAsync(
            int maThanhToan, int adminId, string lyDoTuChoi)
        {
            var payment = await _repo.GetPaymentByIdAsync(maThanhToan);
            if (payment == null)
                return (false, "Phiếu thanh toán không tồn tại!");

            if (payment.TrangThai != "WaitingConfirm")
                return (false, $"Chỉ có thể từ chối phiếu thanh toán ở trạng thái chờ xác nhận!");

            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
                return (false, "Vui lòng nhập lý do từ chối!");

            var result = await _repo.AdminConfirmPaymentAsync(maThanhToan, adminId, false, lyDoTuChoi);

            if (result.Success)
            {
                await _logRepo.LogAsync(adminId, "BOOKING_PAYMENT", maThanhToan.ToString(), "UPDATE",
                    moTa: $"Admin từ chối thanh toán. Lý do: {lyDoTuChoi}");
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu cần xử lý
        /// </summary>
        public async Task<IEnumerable<BookingRequestDTO>> GetAllBookingRequestsAsync(string? trangThai = null)
        {
            return await _repo.GetAllBookingRequestsAsync(trangThai);
        }

        /// <summary>
        /// Lấy yêu cầu của Tenant
        /// </summary>
        public async Task<IEnumerable<BookingRequestDTO>> GetBookingsByTenantAsync(int tenantUserId)
        {
            return await _repo.GetBookingsByTenantAsync(tenantUserId);
        }

        /// <summary>
        /// Lấy chi tiết phiếu thanh toán
        /// </summary>
        public async Task<BookingPayment?> GetPaymentByIdAsync(int maThanhToan)
        {
            return await _repo.GetPaymentByIdAsync(maThanhToan);
        }

        /// <summary>
        /// Đếm yêu cầu cần xử lý
        /// </summary>
        public async Task<(int WaitingConfirm, int PendingApprove)> CountPendingRequestsAsync()
        {
            return await _repo.CountPendingRequestsAsync();
        }
    }
}
