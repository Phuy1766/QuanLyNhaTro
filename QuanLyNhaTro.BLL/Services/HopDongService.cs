using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class HopDongService
    {
        private readonly HopDongRepository _repo = new();
        private readonly PhongTroRepository _phongRepo = new();
        private readonly TaiSanRepository _taiSanRepo = new();
        private readonly HoaDonRepository _hoaDonRepo = new();
        private readonly NotificationRepository _notiRepo = new();
        private readonly ActivityLogRepository _logRepo = new();
        private readonly DamageReportService _damageService = new(); // FIX Issue 4.1

        public async Task<IEnumerable<HopDong>> GetAllAsync(string? trangThai = null)
        {
            return await _repo.GetAllWithDetailsAsync(trangThai);
        }

        public async Task<HopDong?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<HopDong?> GetActiveByPhongAsync(int phongId)
        {
            return await _repo.GetActiveByPhongIdAsync(phongId);
        }

        public async Task<IEnumerable<HopDong>> GetExpiringSoonAsync(int days = 30)
        {
            return await _repo.GetExpiringSoonAsync(days);
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(HopDong hopDong)
        {
            // Validation
            if (hopDong.PhongId <= 0)
                return (false, "Vui lòng chọn phòng!", 0);

            if (hopDong.KhachId <= 0)
                return (false, "Vui lòng chọn khách thuê!", 0);

            if (!ValidationHelper.IsPositive(hopDong.GiaThue))
                return (false, "Giá thuê phải lớn hơn 0!", 0);

            if (!ValidationHelper.IsNonNegative(hopDong.TienCoc))
                return (false, "Tiền cọc không được âm!", 0);

            if (!ValidationHelper.IsEndDateAfterStartDate(hopDong.NgayBatDau, hopDong.NgayKetThuc))
                return (false, "Ngày kết thúc phải sau ngày bắt đầu!", 0);

            // Check phòng đã có hợp đồng chưa
            if (await _phongRepo.HasActiveContractAsync(hopDong.PhongId))
                return (false, "Phòng này đã có hợp đồng!", 0);

            // Generate mã hợp đồng
            hopDong.MaHopDong = await _repo.GenerateMaHopDongAsync();
            hopDong.TrangThai = "Active";
            hopDong.CreatedBy = AuthService.CurrentUser?.UserId;

            var id = await _repo.CreateContractAsync(hopDong);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "HOPDONG", hopDong.MaHopDong, "INSERT",
                    duLieuMoi: hopDong, moTa: $"Tạo hợp đồng {hopDong.MaHopDong}");

                // Tạo thông báo
                await _notiRepo.AddAsync(new Notification
                {
                    LoaiThongBao = "HopDongMoi",
                    TieuDe = "Hợp đồng mới",
                    NoiDung = $"Hợp đồng {hopDong.MaHopDong} đã được tạo"
                });
            }

            return (id > 0, id > 0 ? $"Tạo hợp đồng {hopDong.MaHopDong} thành công!" : "Tạo hợp đồng thất bại!", id);
        }

        public async Task<(bool Success, string Message)> ExtendAsync(int hopDongId, DateTime ngayKetThucMoi, decimal? giaThueMoi = null)
        {
            var hopDong = await _repo.GetByIdAsync(hopDongId);
            if (hopDong == null)
                return (false, "Không tìm thấy hợp đồng!");

            if (hopDong.TrangThai != "Active")
                return (false, "Chỉ có thể gia hạn hợp đồng đang hoạt động!");

            if (ngayKetThucMoi <= hopDong.NgayKetThuc)
                return (false, "Ngày kết thúc mới phải sau ngày kết thúc hiện tại!");

            var result = await _repo.ExtendContractAsync(hopDongId, ngayKetThucMoi, giaThueMoi);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "HOPDONG", hopDong.MaHopDong, "EXTEND",
                    duLieuCu: hopDong, moTa: $"Gia hạn hợp đồng đến {ngayKetThucMoi:dd/MM/yyyy}");
            }

            return (result, result ? "Gia hạn hợp đồng thành công!" : "Gia hạn thất bại!");
        }

        public async Task<(bool Success, string Message, TerminationResult? Result)> TerminateAsync(int hopDongId)
        {
            var hopDong = await _repo.GetByIdAsync(hopDongId);
            if (hopDong == null)
                return (false, "Không tìm thấy hợp đồng!", null);

            if (hopDong.TrangThai != "Active")
                return (false, "Hợp đồng không ở trạng thái hoạt động!", null);

            // ✅ FIX Issue #6: Sử dụng SP mới để tính phí thanh lý sớm
            var fees = await _repo.CalculateTerminationFeesAsync(hopDongId);

            var result = new TerminationResult
            {
                TienCoc = fees.TienCoc,
                CongNoHoaDon = fees.CongNoHoaDon,
                ChiPhiHuHong = fees.ChiPhiHuHong,
                PhiPhatThanhLySom = fees.PhiPhatThanhLySom,
                TongKhauTru = fees.TongKhauTru,
                TienHoanCoc = fees.TienHoanCoc
            };

            return (true, "Đã tính toán thanh lý hợp đồng", result);
        }

        public async Task<(bool Success, string Message)> ConfirmTerminateAsync(int hopDongId, decimal tienHoanCoc, decimal tienKhauTru, string? lyDo)
        {
            var hopDong = await _repo.GetByIdAsync(hopDongId);
            if (hopDong == null)
                return (false, "Không tìm thấy hợp đồng!");

            var result = await _repo.TerminateContractAsync(hopDongId, tienHoanCoc, tienKhauTru, lyDo);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "HOPDONG", hopDong.MaHopDong, "TERMINATE",
                    duLieuCu: hopDong, moTa: $"Thanh lý hợp đồng, hoàn cọc: {tienHoanCoc:N0}");

                await _notiRepo.AddAsync(new Notification
                {
                    LoaiThongBao = "HopDongThanhLy",
                    TieuDe = "Hợp đồng thanh lý",
                    NoiDung = $"Hợp đồng {hopDong.MaHopDong} đã được thanh lý"
                });
            }

            return (result, result ? "Thanh lý hợp đồng thành công!" : "Thanh lý thất bại!");
        }

        public async Task<string> GenerateMaHopDongAsync()
        {
            return await _repo.GenerateMaHopDongAsync();
        }
    }

    public class TerminationResult
    {
        public decimal TienCoc { get; set; }
        public decimal CongNoHoaDon { get; set; }
        public decimal ChiPhiHuHong { get; set; }
        public decimal PhiPhatThanhLySom { get; set; }
        public decimal TongKhauTru { get; set; }
        public decimal TienHoanCoc { get; set; }
    }
}
