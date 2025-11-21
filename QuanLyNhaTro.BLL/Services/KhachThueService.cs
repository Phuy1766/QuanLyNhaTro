using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class KhachThueService
    {
        private readonly KhachThueRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<KhachThue>> GetAllAsync()
        {
            return await _repo.GetAllWithRoomInfoAsync();
        }

        public async Task<IEnumerable<KhachThue>> GetAvailableAsync()
        {
            return await _repo.GetAvailableTenantsAsync();
        }

        public async Task<KhachThue?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<KhachThue>> SearchAsync(string keyword)
        {
            return await _repo.SearchAsync(keyword);
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(KhachThue khach)
        {
            // Validation
            if (!ValidationHelper.IsNotEmpty(khach.HoTen))
                return (false, "Họ tên không được để trống!", 0);

            if (!ValidationHelper.IsValidCCCD(khach.CCCD))
                return (false, "CCCD phải có 12 chữ số!", 0);

            if (!ValidationHelper.IsValidEmail(khach.Email))
                return (false, "Email không hợp lệ!", 0);

            if (!ValidationHelper.IsValidPhone(khach.Phone))
                return (false, "Số điện thoại không hợp lệ!", 0);

            // Check CCCD duplicate
            if (await _repo.CCCDExistsAsync(khach.CCCD))
                return (false, "CCCD đã tồn tại trong hệ thống!", 0);

            // Generate mã khách nếu chưa có
            if (string.IsNullOrEmpty(khach.MaKhach))
                khach.MaKhach = await _repo.GenerateMaKhachAsync();

            // Check mã khách duplicate
            if (await _repo.MaKhachExistsAsync(khach.MaKhach))
                return (false, "Mã khách đã tồn tại!", 0);

            khach.IsActive = true;

            var id = await _repo.InsertAsync(khach);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "KHACHTHUE", khach.MaKhach, "INSERT",
                    duLieuMoi: khach, moTa: $"Thêm khách thuê {khach.HoTen}");
            }

            return (id > 0, id > 0 ? "Thêm khách thuê thành công!" : "Thêm khách thuê thất bại!", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(KhachThue khach)
        {
            // Validation
            if (!ValidationHelper.IsNotEmpty(khach.HoTen))
                return (false, "Họ tên không được để trống!");

            if (!ValidationHelper.IsValidCCCD(khach.CCCD))
                return (false, "CCCD phải có 12 chữ số!");

            if (!ValidationHelper.IsValidEmail(khach.Email))
                return (false, "Email không hợp lệ!");

            if (!ValidationHelper.IsValidPhone(khach.Phone))
                return (false, "Số điện thoại không hợp lệ!");

            // Check CCCD duplicate
            if (await _repo.CCCDExistsAsync(khach.CCCD, khach.KhachId))
                return (false, "CCCD đã tồn tại trong hệ thống!");

            var oldKhach = await _repo.GetByIdAsync(khach.KhachId);
            var result = await _repo.UpdateAsync(khach);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "KHACHTHUE", khach.MaKhach, "UPDATE",
                    duLieuCu: oldKhach, duLieuMoi: khach, moTa: $"Cập nhật khách thuê {khach.HoTen}");
            }

            return (result, result ? "Cập nhật thành công!" : "Cập nhật thất bại!");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int khachId)
        {
            var khach = await _repo.GetByIdAsync(khachId);
            if (khach == null)
                return (false, "Không tìm thấy khách thuê!");

            // Check có hợp đồng active không
            if (await _repo.HasActiveContractAsync(khachId))
                return (false, "Không thể xóa khách đang có hợp đồng!");

            var result = await _repo.DeleteAsync(khachId);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "KHACHTHUE", khach.MaKhach, "DELETE",
                    duLieuCu: khach, moTa: $"Xóa khách thuê {khach.HoTen}");
            }

            return (result, result ? "Xóa thành công!" : "Xóa thất bại!");
        }

        public async Task<string> GenerateMaKhachAsync()
        {
            return await _repo.GenerateMaKhachAsync();
        }
    }
}
