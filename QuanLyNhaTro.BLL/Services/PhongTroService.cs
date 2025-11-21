using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class PhongTroService
    {
        private readonly PhongTroRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<PhongTro>> GetAllAsync(int? buildingId = null, string? trangThai = null)
        {
            return await _repo.GetAllWithDetailsAsync(buildingId, trangThai);
        }

        public async Task<IEnumerable<PhongTro>> GetAvailableAsync(int? buildingId = null)
        {
            return await _repo.GetAvailableRoomsAsync(buildingId);
        }

        public async Task<PhongTro?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<LoaiPhong>> GetLoaiPhongAsync()
        {
            return await _repo.GetLoaiPhongAsync();
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(PhongTro phong)
        {
            // Validation
            if (!ValidationHelper.IsNotEmpty(phong.MaPhong))
                return (false, "Mã phòng không được để trống!", 0);

            if (phong.BuildingId <= 0)
                return (false, "Vui lòng chọn tòa nhà!", 0);

            if (!ValidationHelper.IsPositive(phong.GiaThue))
                return (false, "Giá thuê phải lớn hơn 0!", 0);

            // Check duplicate
            if (await _repo.MaPhongExistsAsync(phong.MaPhong, phong.BuildingId))
                return (false, "Mã phòng đã tồn tại trong tòa nhà này!", 0);

            phong.IsActive = true;
            phong.TrangThai = "Trống";

            var id = await _repo.InsertAsync(phong);

            if (id > 0)
            {
                // Lưu lịch sử giá ban đầu
                await _repo.UpdateGiaThueAsync(id, phong.GiaThue, AuthService.CurrentUser?.UserId ?? 0, "Giá khởi tạo");

                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "PHONGTRO", phong.MaPhong, "INSERT",
                    duLieuMoi: phong, moTa: $"Thêm phòng {phong.MaPhong}");
            }

            return (id > 0, id > 0 ? "Thêm phòng thành công!" : "Thêm phòng thất bại!", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(PhongTro phong)
        {
            // Validation
            if (!ValidationHelper.IsNotEmpty(phong.MaPhong))
                return (false, "Mã phòng không được để trống!");

            if (!ValidationHelper.IsPositive(phong.GiaThue))
                return (false, "Giá thuê phải lớn hơn 0!");

            // Check duplicate
            if (await _repo.MaPhongExistsAsync(phong.MaPhong, phong.BuildingId, phong.PhongId))
                return (false, "Mã phòng đã tồn tại trong tòa nhà này!");

            var oldPhong = await _repo.GetByIdAsync(phong.PhongId);

            // Nếu giá thay đổi, lưu lịch sử
            if (oldPhong != null && oldPhong.GiaThue != phong.GiaThue)
            {
                await _repo.UpdateGiaThueAsync(phong.PhongId, phong.GiaThue,
                    AuthService.CurrentUser?.UserId ?? 0, "Cập nhật giá");
            }

            var result = await _repo.UpdateAsync(phong);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "PHONGTRO", phong.MaPhong, "UPDATE",
                    duLieuCu: oldPhong, duLieuMoi: phong, moTa: $"Cập nhật phòng {phong.MaPhong}");
            }

            return (result, result ? "Cập nhật thành công!" : "Cập nhật thất bại!");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int phongId)
        {
            var phong = await _repo.GetByIdAsync(phongId);
            if (phong == null)
                return (false, "Không tìm thấy phòng!");

            // Check có hợp đồng active không
            if (await _repo.HasActiveContractAsync(phongId))
                return (false, "Không thể xóa phòng đang có hợp đồng!");

            var result = await _repo.DeleteAsync(phongId);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "PHONGTRO", phong.MaPhong, "DELETE",
                    duLieuCu: phong, moTa: $"Xóa phòng {phong.MaPhong}");
            }

            return (result, result ? "Xóa thành công!" : "Xóa thất bại!");
        }

        public async Task<IEnumerable<LichSuGia>> GetLichSuGiaAsync(int phongId)
        {
            return await _repo.GetLichSuGiaAsync(phongId);
        }
    }
}
