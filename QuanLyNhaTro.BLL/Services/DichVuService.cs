using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class DichVuService
    {
        private readonly DichVuRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<DichVu>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<DichVu?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<DichVu>> GetChiSoServicesAsync()
        {
            return await _repo.GetChiSoServicesAsync();
        }

        public async Task<IEnumerable<DichVu>> GetFixedServicesAsync()
        {
            return await _repo.GetFixedServicesAsync();
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(DichVu dichVu)
        {
            if (!ValidationHelper.IsNotEmpty(dichVu.MaDichVu))
                return (false, "Mã dịch vụ không được để trống!", 0);

            if (!ValidationHelper.IsNotEmpty(dichVu.TenDichVu))
                return (false, "Tên dịch vụ không được để trống!", 0);

            if (!ValidationHelper.IsPositive(dichVu.DonGia))
                return (false, "Đơn giá phải lớn hơn 0!", 0);

            if (await _repo.MaDichVuExistsAsync(dichVu.MaDichVu))
                return (false, "Mã dịch vụ đã tồn tại!", 0);

            dichVu.IsActive = true;
            var id = await _repo.InsertAsync(dichVu);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "DICHVU", dichVu.MaDichVu, "INSERT",
                    duLieuMoi: dichVu, moTa: $"Thêm dịch vụ {dichVu.TenDichVu}");
            }

            return (id > 0, id > 0 ? "Thêm dịch vụ thành công!" : "Thêm dịch vụ thất bại!", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(DichVu dichVu)
        {
            if (!ValidationHelper.IsNotEmpty(dichVu.TenDichVu))
                return (false, "Tên dịch vụ không được để trống!");

            if (!ValidationHelper.IsPositive(dichVu.DonGia))
                return (false, "Đơn giá phải lớn hơn 0!");

            if (await _repo.MaDichVuExistsAsync(dichVu.MaDichVu, dichVu.DichVuId))
                return (false, "Mã dịch vụ đã tồn tại!");

            var oldDichVu = await _repo.GetByIdAsync(dichVu.DichVuId);
            var result = await _repo.UpdateAsync(dichVu);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "DICHVU", dichVu.MaDichVu, "UPDATE",
                    duLieuCu: oldDichVu, duLieuMoi: dichVu, moTa: $"Cập nhật dịch vụ {dichVu.TenDichVu}");
            }

            return (result, result ? "Cập nhật thành công!" : "Cập nhật thất bại!");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int dichVuId)
        {
            var dichVu = await _repo.GetByIdAsync(dichVuId);
            if (dichVu == null)
                return (false, "Không tìm thấy dịch vụ!");

            var result = await _repo.DeleteAsync(dichVuId);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "DICHVU", dichVu.MaDichVu, "DELETE",
                    duLieuCu: dichVu, moTa: $"Xóa dịch vụ {dichVu.TenDichVu}");
            }

            return (result, result ? "Xóa thành công!" : "Xóa thất bại!");
        }
    }
}
