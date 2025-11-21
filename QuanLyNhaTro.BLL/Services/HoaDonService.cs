using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class HoaDonService
    {
        private readonly HoaDonRepository _repo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly DichVuRepository _dichVuRepo = new();
        private readonly NotificationRepository _notiRepo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<HoaDon>> GetAllAsync(string? trangThai = null, int? year = null, int? month = null)
        {
            return await _repo.GetAllWithDetailsAsync(trangThai, year, month);
        }

        public async Task<HoaDon?> GetByIdAsync(int id)
        {
            return await _repo.GetWithDetailsAsync(id);
        }

        public async Task<IEnumerable<HoaDon>> GetUnpaidAsync()
        {
            return await _repo.GetUnpaidAsync();
        }

        public async Task<IEnumerable<HoaDon>> GetOverdueAsync()
        {
            return await _repo.GetOverdueAsync();
        }

        public async Task<IEnumerable<HoaDon>> GetByHopDongAsync(int hopDongId)
        {
            return await _repo.GetByHopDongAsync(hopDongId);
        }

        /// <summary>
        /// Tạo hóa đơn cho một hợp đồng
        /// </summary>
        public async Task<(bool Success, string Message, int Id)> CreateAsync(int hopDongId, DateTime thangNam, List<ChiTietHoaDon> chiTietDichVu)
        {
            var hopDong = await _hopDongRepo.GetByIdAsync(hopDongId);
            if (hopDong == null)
                return (false, "Không tìm thấy hợp đồng!", 0);

            if (hopDong.TrangThai != "Active")
                return (false, "Hợp đồng không còn hoạt động!", 0);

            // Check đã có hóa đơn tháng này chưa
            if (await _repo.ExistsForMonthAsync(hopDongId, thangNam))
                return (false, "Đã có hóa đơn cho tháng này!", 0);

            // Tính tổng tiền dịch vụ
            decimal tongDichVu = chiTietDichVu.Sum(x => x.ThanhTien);
            decimal tongCong = hopDong.GiaThue + tongDichVu;

            var hoaDon = new HoaDon
            {
                MaHoaDon = await _repo.GenerateMaHoaDonAsync(thangNam),
                HopDongId = hopDongId,
                ThangNam = new DateTime(thangNam.Year, thangNam.Month, 1),
                TienPhong = hopDong.GiaThue,
                TongTienDichVu = tongDichVu,
                TongCong = tongCong,
                DaThanhToan = 0,
                ConNo = tongCong,
                NgayHetHan = new DateTime(thangNam.Year, thangNam.Month, 1).AddDays(10),
                TrangThai = "ChuaThanhToan",
                CreatedBy = AuthService.CurrentUser?.UserId
            };

            var id = await _repo.CreateWithDetailsAsync(hoaDon, chiTietDichVu);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "HOADON", hoaDon.MaHoaDon, "INSERT",
                    duLieuMoi: hoaDon, moTa: $"Tạo hóa đơn {hoaDon.MaHoaDon}");
            }

            return (id > 0, id > 0 ? $"Tạo hóa đơn {hoaDon.MaHoaDon} thành công!" : "Tạo hóa đơn thất bại!", id);
        }

        /// <summary>
        /// Tạo hóa đơn hàng loạt cho tất cả hợp đồng active
        /// </summary>
        public async Task<(int Success, int Failed, string Message)> CreateBatchAsync(DateTime thangNam)
        {
            var hopDongs = await _hopDongRepo.GetAllWithDetailsAsync("Active");
            var dichVuCoDinh = (await _dichVuRepo.GetFixedServicesAsync()).ToList();

            int success = 0, failed = 0;

            foreach (var hd in hopDongs)
            {
                try
                {
                    // Check đã có hóa đơn chưa
                    if (await _repo.ExistsForMonthAsync(hd.HopDongId, thangNam))
                    {
                        failed++;
                        continue;
                    }

                    // Tạo chi tiết dịch vụ cố định
                    var chiTiet = dichVuCoDinh.Select(dv => new ChiTietHoaDon
                    {
                        DichVuId = dv.DichVuId,
                        SoLuong = 1,
                        DonGia = dv.DonGia,
                        ThanhTien = dv.DonGia
                    }).ToList();

                    var result = await CreateAsync(hd.HopDongId, thangNam, chiTiet);
                    if (result.Success) success++;
                    else failed++;
                }
                catch
                {
                    failed++;
                }
            }

            return (success, failed, $"Đã tạo {success} hóa đơn, {failed} thất bại");
        }

        /// <summary>
        /// Thanh toán hóa đơn
        /// </summary>
        public async Task<(bool Success, string Message)> PaymentAsync(int hoaDonId, decimal soTien)
        {
            var hoaDon = await _repo.GetByIdAsync(hoaDonId);
            if (hoaDon == null)
                return (false, "Không tìm thấy hóa đơn!");

            if (hoaDon.TrangThai == "DaThanhToan")
                return (false, "Hóa đơn đã được thanh toán!");

            if (soTien <= 0)
                return (false, "Số tiền thanh toán phải lớn hơn 0!");

            if (soTien > hoaDon.ConNo)
                return (false, $"Số tiền thanh toán vượt quá công nợ ({hoaDon.ConNo:N0})!");

            var result = await _repo.PaymentAsync(hoaDonId, soTien);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "HOADON", hoaDon.MaHoaDon, "PAYMENT",
                    moTa: $"Thanh toán {soTien:N0} cho hóa đơn {hoaDon.MaHoaDon}");

                await _notiRepo.AddAsync(new Notification
                {
                    LoaiThongBao = "ThanhToanHoaDon",
                    TieuDe = "Thanh toán hóa đơn",
                    NoiDung = $"Hóa đơn {hoaDon.MaHoaDon} đã thanh toán {soTien:N0} VNĐ"
                });
            }

            return (result, result ? "Thanh toán thành công!" : "Thanh toán thất bại!");
        }

        /// <summary>
        /// Tính chi tiết dịch vụ theo chỉ số
        /// </summary>
        public ChiTietHoaDon CalculateServiceDetail(DichVu dichVu, decimal? chiSoCu, decimal? chiSoMoi)
        {
            decimal soLuong = 0;
            if (chiSoCu.HasValue && chiSoMoi.HasValue)
                soLuong = chiSoMoi.Value - chiSoCu.Value;

            return new ChiTietHoaDon
            {
                DichVuId = dichVu.DichVuId,
                ChiSoCu = chiSoCu,
                ChiSoMoi = chiSoMoi,
                SoLuong = soLuong,
                DonGia = dichVu.DonGia,
                ThanhTien = soLuong * dichVu.DonGia,
                TenDichVu = dichVu.TenDichVu,
                DonViTinh = dichVu.DonViTinh
            };
        }
    }
}
