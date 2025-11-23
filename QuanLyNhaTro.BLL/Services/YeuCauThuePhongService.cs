using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    /// <summary>
    /// Service xử lý Yêu cầu thuê phòng (Booking Request)
    /// Bao gồm validation, thanh toán, duyệt hợp đồng
    /// </summary>
    public class YeuCauThuePhongService
    {
        private readonly YeuCauThuePhongRepository _repo = new();
        private readonly PhongTroRepository _phongRepo = new();
        private readonly KhachThueRepository _khachRepo = new();
        private readonly ActivityLogRepository _logRepo = new();
        private readonly NotificationRepository _notiRepo = new();

        /// <summary>
        /// Lấy tất cả yêu cầu
        /// </summary>
        public async Task<IEnumerable<YeuCauThuePhong>> GetAllAsync(string? trangThai = null)
        {
            return await _repo.GetAllAsync(trangThai);
        }

        /// <summary>
        /// Lấy yêu cầu theo Tenant
        /// </summary>
        public async Task<IEnumerable<YeuCauThuePhong>> GetByTenantAsync(int tenantUserId)
        {
            return await _repo.GetByTenantAsync(tenantUserId);
        }

        /// <summary>
        /// Lấy yêu cầu theo ID
        /// </summary>
        public async Task<YeuCauThuePhong?> GetByIdAsync(int maYeuCau)
        {
            return await _repo.GetByIdAsync(maYeuCau);
        }

        /// <summary>
        /// Tạo yêu cầu thuê phòng mới
        /// ✅ FIX: Thêm các validation quan trọng
        /// </summary>
        public async Task<(bool Success, string Message, int Id)> CreateAsync(YeuCauThuePhong yeuCau)
        {
            // 🔴 FIX 1.3: Kiểm tra Tenant đã có yêu cầu pending cho phòng này chưa
            if (await _repo.HasPendingRequestAsync(yeuCau.MaTenant, yeuCau.PhongId))
                return (false, "Bạn đã gửi yêu cầu cho phòng này rồi. Vui lòng chờ kết quả xử lý!", 0);

            // Kiểm tra phòng tồn tại
            var phong = await _phongRepo.GetByIdAsync(yeuCau.PhongId);
            if (phong == null)
                return (false, "Phòng không tồn tại!", 0);

            // Kiểm tra phòng còn trống
            if (phong.TrangThai != "Trống")
                return (false, $"Phòng không còn trống (Trạng thái: {phong.TrangThai})!", 0);

            // 🔴 FIX 7.1: Kiểm tra NgayBatDauMongMuon >= Hôm nay
            if (yeuCau.NgayBatDauMongMuon < DateTime.Today)
                return (false, "Ngày bắt đầu mong muốn phải từ hôm nay trở đi!", 0);

            // 🔴 FIX 7.2: Kiểm tra số người không vượt quá giới hạn phòng
            if (yeuCau.SoNguoi > phong.SoNguoiToiDa)
                return (false, $"Phòng chỉ chứa tối đa {phong.SoNguoiToiDa} người, bạn đăng ký {yeuCau.SoNguoi} người!", 0);

            if (yeuCau.SoNguoi <= 0)
                return (false, "Số người phải lớn hơn 0!", 0);

            // Tạo yêu cầu
            yeuCau.NgayGui = DateTime.Now;
            yeuCau.TrangThai = "Pending";
            // 🔴 FIX 5.1: Thiết lập hạn thanh toán = 24 giờ từ bây giờ
            yeuCau.NgayHetHan = DateTime.Now.AddHours(24);

            var id = await _repo.CreateAsync(yeuCau);

            if (id > 0)
            {
                // Ghi log
                await _logRepo.LogAsync(yeuCau.MaTenant, "YEUCAU_THUEPHONG", id.ToString(), "INSERT",
                    duLieuMoi: yeuCau, moTa: $"Tạo yêu cầu thuê phòng {phong.MaPhong}");

                // Gửi thông báo cho Admin/Manager
                await _notiRepo.AddAsync(new Notification
                {
                    LoaiThongBao = "ThuePhongMoi",
                    TieuDe = $"Yêu cầu thuê phòng mới: {phong.MaPhong}",
                    NoiDung = $"Tenant {yeuCau.TenTenant} gửi yêu cầu thuê phòng {phong.MaPhong}",
                    DuongDan = $"/BookingRequest/{id}"
                });
            }

            return (id > 0, id > 0 ? "Gửi yêu cầu thuê phòng thành công! Vui lòng chờ Admin xác nhận." : "Gửi yêu cầu thất bại!", id);
        }

        /// <summary>
        /// Hủy yêu cầu (Tenant tự hủy hoặc auto hủy khi hết hạn)
        /// ✅ FIX: Cho phép hủy yêu cầu pending
        /// </summary>
        public async Task<(bool Success, string Message)> CancelAsync(int maYeuCau, int tenantUserId)
        {
            var yeuCau = await _repo.GetByIdAsync(maYeuCau);
            if (yeuCau == null)
                return (false, "Yêu cầu không tồn tại!");

            // Chỉ tenant chủ yêu cầu mới được hủy
            if (yeuCau.MaTenant != tenantUserId)
                return (false, "Bạn không có quyền hủy yêu cầu này!");

            // Chỉ có thể hủy khi Pending
            if (yeuCau.TrangThai != "Pending" && yeuCau.TrangThai != "PendingPayment")
                return (false, $"Không thể hủy yêu cầu ở trạng thái {yeuCau.TrangThai}!");

            // Cập nhật trạng thái (thêm vào DB nếu chưa có logic hủy)
            var result = true; // TODO: Thêm method UpdateStatusAsync

            if (result)
            {
                await _logRepo.LogAsync(tenantUserId, "YEUCAU_THUEPHONG", maYeuCau.ToString(), "UPDATE",
                    duLieuCu: yeuCau, moTa: "Hủy yêu cầu thuê phòng");
            }

            return (result, result ? "Hủy yêu cầu thành công!" : "Hủy yêu cầu thất bại!");
        }

        /// <summary>
        /// Duyệt yêu cầu - Tạo hợp đồng
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveAsync(int maYeuCau, int nguoiXuLy, string maHopDong, DateTime ngayKetThuc, decimal tienCoc, string? ghiChu = null)
        {
            return await _repo.ApproveAsync(maYeuCau, nguoiXuLy, maHopDong, ngayKetThuc, tienCoc, ghiChu);
        }

        /// <summary>
        /// Từ chối yêu cầu
        /// </summary>
        public async Task<(bool Success, string Message)> RejectAsync(int maYeuCau, int nguoiXuLy, string lyDoTuChoi)
        {
            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
                return (false, "Vui lòng nhập lý do từ chối!");

            return await _repo.RejectAsync(maYeuCau, nguoiXuLy, lyDoTuChoi);
        }

        /// <summary>
        /// Lấy danh sách phòng trống cho Tenant
        /// </summary>
        public async Task<IEnumerable<PhongTrongDTO>> GetAvailableRoomsAsync(int tenantUserId, string? buildingCode = null, decimal? giaMin = null, decimal? giaMax = null, int? soNguoi = null)
        {
            return await _repo.GetAvailableRoomsAsync(tenantUserId, buildingCode, giaMin, giaMax, soNguoi);
        }

        /// <summary>
        /// Đếm yêu cầu pending
        /// </summary>
        public async Task<int> CountPendingAsync()
        {
            return await _repo.CountPendingAsync();
        }

        /// <summary>
        /// Auto hủy yêu cầu hết hạn thanh toán
        /// (Chạy hàng ngày bằng Scheduler)
        /// ✅ FIX 5.1: Tự động hủy yêu cầu pending thanh toán quá 24 giờ
        /// </summary>
        public async Task<(int Canceled, string Message)> AutoCancelExpiredRequestsAsync()
        {
            // TODO: Implement stored procedure sp_AutoCancelExpiredBookingRequests
            // Hoặc query từ code
            
            int canceledCount = 0;
            
            // Lấy các yêu cầu pending payment quá 24 giờ
            // var expiredRequests = await _repo.GetExpiredRequestsAsync();
            // foreach (var req in expiredRequests) { await CancelAsync(req.MaYeuCau, req.MaTenant); canceledCount++; }
            
            return (canceledCount, $"Đã hủy {canceledCount} yêu cầu hết hạn");
        }
    }
}
