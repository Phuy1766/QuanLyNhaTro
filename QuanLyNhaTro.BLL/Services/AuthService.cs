using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    /// <summary>
    /// Service xử lý đăng nhập, đăng xuất, quản lý session
    /// </summary>
    public class AuthService
    {
        private readonly UserRepository _userRepo = new();
        private readonly ActivityLogRepository _logRepo = new();

        /// <summary>
        /// User hiện tại đang đăng nhập
        /// </summary>
        public static User? CurrentUser { get; private set; }

        /// <summary>
        /// Kiểm tra đã đăng nhập chưa
        /// </summary>
        public static bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// Kiểm tra quyền Admin
        /// </summary>
        public static bool IsAdmin => CurrentUser?.RoleName == "Admin";

        /// <summary>
        /// Kiểm tra quyền Manager
        /// </summary>
        public static bool IsManager => CurrentUser?.RoleName == "Manager" || IsAdmin;

        /// <summary>
        /// Kiểm tra quyền Tenant
        /// </summary>
        public static bool IsTenant => CurrentUser?.RoleName == "Tenant";

        /// <summary>
        /// Đăng nhập
        /// </summary>
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Vui lòng nhập tên đăng nhập và mật khẩu!");

            // Lấy user theo username và verify password (hỗ trợ migration legacy SHA256 -> PBKDF2)
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return (false, "Tên đăng nhập không tồn tại!");
            if (!user.IsActive)
                return (false, "Tài khoản đã bị khóa!");

            // Verify mật khẩu (hàm sẽ hỗ trợ cả format legacy và PBKDF2)
            var verified = PasswordHelper.VerifyPassword(password, user.PasswordHash);
            if (!verified)
                return (false, "Mật khẩu không đúng!");

            // Nếu là legacy hash (SHA256), migrate lên PBKDF2 ngay lập tức
            if (PasswordHelper.IsLegacyHash(user.PasswordHash))
            {
                var newHash = PasswordHelper.HashPassword(password);
                var changed = await _userRepo.ChangePasswordAsync(user.UserId, newHash);
                if (changed)
                {
                    user.PasswordHash = newHash;
                }
            }

            CurrentUser = user;

            // Ghi log
            await _logRepo.LogAsync(user.UserId, "USERS", user.UserId.ToString(), "LOGIN",
                moTa: $"User {user.Username} đăng nhập");

            return (true, $"Xin chào {user.FullName}!");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        public async Task LogoutAsync()
        {
            if (CurrentUser != null)
            {
                await _logRepo.LogAsync(CurrentUser.UserId, "USERS", CurrentUser.UserId.ToString(), "LOGOUT",
                    moTa: $"User {CurrentUser.Username} đăng xuất");
            }
            CurrentUser = null;
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)
        {
            if (CurrentUser == null)
                return (false, "Chưa đăng nhập!");

            if (string.IsNullOrWhiteSpace(oldPassword))
                return (false, "Vui lòng nhập mật khẩu cũ!");

            if (string.IsNullOrWhiteSpace(newPassword))
                return (false, "Vui lòng nhập mật khẩu mới!");

            if (newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự!");

            if (newPassword != confirmPassword)
                return (false, "Mật khẩu xác nhận không khớp!");

            // Verify old password (hỗ trợ legacy)
            var okOld = PasswordHelper.VerifyPassword(oldPassword, CurrentUser.PasswordHash);
            if (!okOld) return (false, "Mật khẩu cũ không đúng!");

            var newHash = PasswordHelper.HashPassword(newPassword);
            var result = await _userRepo.ChangePasswordAsync(CurrentUser.UserId, newHash);

            if (result)
            {
                CurrentUser.PasswordHash = newHash;
                await _logRepo.LogAsync(CurrentUser.UserId, "USERS", CurrentUser.UserId.ToString(), "CHANGE_PASSWORD",
                    moTa: "Đổi mật khẩu thành công");
            }

            return (result, result ? "Đổi mật khẩu thành công!" : "Đổi mật khẩu thất bại!");
        }

        /// <summary>
        /// Reset mật khẩu (cho Admin)
        /// </summary>
        public async Task<(bool Success, string Message, string? NewPassword)> ResetPasswordAsync(int userId)
        {
            if (!IsAdmin)
                return (false, "Không có quyền thực hiện!", null);

            var newPassword = PasswordHelper.GenerateRandomPassword();
            var hash = PasswordHelper.HashPassword(newPassword);
            var result = await _userRepo.ChangePasswordAsync(userId, hash);

            if (result)
            {
                await _logRepo.LogAsync(CurrentUser!.UserId, "USERS", userId.ToString(), "RESET_PASSWORD",
                    moTa: $"Reset mật khẩu cho user ID {userId}");
            }

            return (result, result ? "Reset mật khẩu thành công!" : "Reset mật khẩu thất bại!", newPassword);
        }

        /// <summary>
        /// Cập nhật theme
        /// </summary>
        public async Task<bool> UpdateThemeAsync(string theme)
        {
            if (CurrentUser == null) return false;

            var result = await _userRepo.UpdateThemeAsync(CurrentUser.UserId, theme);
            if (result)
                CurrentUser.Theme = theme;

            return result;
        }
    }
}
