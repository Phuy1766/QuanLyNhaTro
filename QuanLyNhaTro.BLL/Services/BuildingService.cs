using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class BuildingService
    {
        private readonly BuildingRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<Building>> GetAllAsync()
        {
            return await _repo.GetAllWithStatsAsync();
        }

        public async Task<Building?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(Building building)
        {
            if (!ValidationHelper.IsNotEmpty(building.BuildingCode))
                return (false, "Mã tòa nhà không được để trống!", 0);

            if (!ValidationHelper.IsNotEmpty(building.BuildingName))
                return (false, "Tên tòa nhà không được để trống!", 0);

            if (await _repo.CodeExistsAsync(building.BuildingCode))
                return (false, "Mã tòa nhà đã tồn tại!", 0);

            building.IsActive = true;
            var id = await _repo.InsertAsync(building);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BUILDING", building.BuildingCode, "INSERT",
                    duLieuMoi: building, moTa: $"Thêm tòa nhà {building.BuildingName}");
            }

            return (id > 0, id > 0 ? "Thêm tòa nhà thành công!" : "Thêm tòa nhà thất bại!", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Building building)
        {
            if (!ValidationHelper.IsNotEmpty(building.BuildingCode))
                return (false, "Mã tòa nhà không được để trống!");

            if (!ValidationHelper.IsNotEmpty(building.BuildingName))
                return (false, "Tên tòa nhà không được để trống!");

            if (await _repo.CodeExistsAsync(building.BuildingCode, building.BuildingId))
                return (false, "Mã tòa nhà đã tồn tại!");

            var oldBuilding = await _repo.GetByIdAsync(building.BuildingId);
            var result = await _repo.UpdateAsync(building);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BUILDING", building.BuildingCode, "UPDATE",
                    duLieuCu: oldBuilding, duLieuMoi: building, moTa: $"Cập nhật tòa nhà {building.BuildingName}");
            }

            return (result, result ? "Cập nhật thành công!" : "Cập nhật thất bại!");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int buildingId)
        {
            var building = await _repo.GetByIdAsync(buildingId);
            if (building == null)
                return (false, "Không tìm thấy tòa nhà!");

            if (await _repo.HasRoomsAsync(buildingId))
                return (false, "Không thể xóa tòa nhà đang có phòng!");

            var result = await _repo.DeleteAsync(buildingId);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BUILDING", building.BuildingCode, "DELETE",
                    duLieuCu: building, moTa: $"Xóa tòa nhà {building.BuildingName}");
            }

            return (result, result ? "Xóa thành công!" : "Xóa thất bại!");
        }
    }
}
