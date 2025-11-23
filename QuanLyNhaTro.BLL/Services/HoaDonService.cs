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

            // ✅ FIX: Không cho tạo hóa đơn cho tháng sau khi hợp đồng hết hạn
            var thangNamChuanHoa = new DateTime(thangNam.Year, thangNam.Month, 1);
            if (thangNamChuanHoa > new DateTime(hopDong.NgayKetThuc.Year, hopDong.NgayKetThuc.Month, 1))
                return (false, $"Không thể tạo hóa đơn cho tháng {thangNam:MM/yyyy}. Hợp đồng hết hạn {hopDong.NgayKetThuc:dd/MM/yyyy}!", 0);

            // Check đã có hóa đơn tháng này chưa
            if (await _repo.ExistsForMonthAsync(hopDongId, thangNam))
                return (false, "Đã có hóa đơn cho tháng này!", 0);

            // Tính tổng tiền dịch vụ
            decimal tongDichVu = chiTietDichVu.Sum(x => x.ThanhTien);
            decimal tongCong = hopDong.GiaThue + tongDichVu;

            // ✅ FIX: Tính NgayHetHan hợp lý
            // Nếu tạo hóa đơn tháng hiện tại, cho hạn thanh toán từ ngày tạo + 10 ngày
            // Nếu tạo hóa đơn tháng tương lai, cho hạn = ngày 10 của tháng đó
            DateTime ngayHetHan;
            var thangHienTai = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            if (thangNamChuanHoa == thangHienTai)
            {
                // Hóa đơn tháng hiện tại: cho 10 ngày kể từ hôm nay
                ngayHetHan = DateTime.Now.Date.AddDays(10);
            }
            else if (thangNamChuanHoa < thangHienTai)
            {
                // Hóa đơn tháng quá khứ: hết hạn ngay
                ngayHetHan = DateTime.Now.Date.AddDays(3); // Cho 3 ngày gia hạn
            }
            else
            {
                // Hóa đơn tháng tương lai: ngày 10 của tháng đó
                ngayHetHan = new DateTime(thangNam.Year, thangNam.Month, 10);
            }

            var hoaDon = new HoaDon
            {
                MaHoaDon = await _repo.GenerateMaHoaDonAsync(thangNam),
                HopDongId = hopDongId,
                ThangNam = thangNamChuanHoa,
                TienPhong = hopDong.GiaThue,
                TongTienDichVu = tongDichVu,
                TongCong = tongCong,
                DaThanhToan = 0,
                ConNo = tongCong,
                NgayHetHan = ngayHetHan,
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
            
            // ✅ FIX #2: Chỉ tạo hóa đơn cho hợp đồng chưa hết hạn
            hopDongs = hopDongs.Where(hd => hd.NgayKetThuc >= thangNam).ToList();
            
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
        /// Xóa hóa đơn (chỉ cho phép xóa hóa đơn chưa thanh toán)
        /// </summary>
        public async Task<bool> DeleteAsync(int hoaDonId)
        {
            var hoaDon = await _repo.GetByIdAsync(hoaDonId);
            if (hoaDon == null)
                return false;

            // Chỉ cho phép xóa hóa đơn chưa thanh toán
            if (hoaDon.TrangThai == "DaThanhToan")
                return false;

            var result = await _repo.DeleteAsync(hoaDonId);

            if (result && AuthService.CurrentUser != null)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser.UserId, "HOADON", hoaDon.MaHoaDon, "DELETE",
                    moTa: $"Xóa hóa đơn {hoaDon.MaHoaDon} - Tháng {hoaDon.ThangNam:MM/yyyy}");
            }

            return result;
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
