using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class UserService
    {
        private readonly UserRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _repo.GetAllWithRoleAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            return await _repo.GetAllRolesAsync();
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(User user, string password)
        {
            // Validation
            if (!ValidationHelper.IsNotEmpty(user.Username))
                return (false, "Tên đăng nhập không được để trống!", 0);

            if (!ValidationHelper.IsNotEmpty(user.FullName))
                return (false, "Họ tên không được để trống!", 0);

            if (password.Length < 6)
                return (false, "Mật khẩu phải có ít nhất 6 ký tự!", 0);

            if (!ValidationHelper.IsValidEmail(user.Email))
                return (false, "Email không hợp lệ!", 0);

            if (!ValidationHelper.IsValidPhone(user.Phone))
                return (false, "Số điện thoại không hợp lệ!", 0);

            // Check username exists
            if (await _repo.UsernameExistsAsync(user.Username))
                return (false, "Tên đăng nhập đã tồn tại!", 0);

            user.PasswordHash = PasswordHelper.HashPassword(password);
            user.IsActive = true;

            var id = await _repo.InsertAsync(user);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "USERS", id.ToString(), "INSERT",
                    duLieuMoi: user, moTa: $"Tạo tài khoản {user.Username}");
            }

            return (id > 0, id > 0 ? "Tạo tài khoản thành công!" : "Tạo tài khoản thất bại!", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(User user)
        {
            // Validation
            if (!ValidationHelper.IsNotEmpty(user.FullName))
                return (false, "Họ tên không được để trống!");

            if (!ValidationHelper.IsValidEmail(user.Email))
                return (false, "Email không hợp lệ!");

            if (!ValidationHelper.IsValidPhone(user.Phone))
                return (false, "Số điện thoại không hợp lệ!");

            var oldUser = await _repo.GetByIdAsync(user.UserId);
            var result = await _repo.UpdateAsync(user);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "USERS", user.UserId.ToString(), "UPDATE",
                    duLieuCu: oldUser, duLieuMoi: user, moTa: $"Cập nhật tài khoản {user.Username}");
            }

            return (result, result ? "Cập nhật thành công!" : "Cập nhật thất bại!");
        }

        public async Task<(bool Success, string Message)> ToggleActiveAsync(int userId)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy tài khoản!");

            if (userId == AuthService.CurrentUser?.UserId)
                return (false, "Không thể khóa tài khoản của chính mình!");

            var result = await _repo.ToggleActiveAsync(userId);

            if (result)
            {
                var action = user.IsActive ? "Khóa" : "Mở khóa";
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "USERS", userId.ToString(), "TOGGLE_ACTIVE",
                    moTa: $"{action} tài khoản {user.Username}");
            }

            return (result, result ? "Thao tác thành công!" : "Thao tác thất bại!");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int userId)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy tài khoản!");

            if (userId == AuthService.CurrentUser?.UserId)
                return (false, "Không thể xóa tài khoản của chính mình!");

            var result = await _repo.DeleteAsync(userId);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "USERS", userId.ToString(), "DELETE",
                    duLieuCu: user, moTa: $"Xóa tài khoản {user.Username}");
            }

            return (result, result ? "Xóa thành công!" : "Xóa thất bại!");
        }
    }
}
