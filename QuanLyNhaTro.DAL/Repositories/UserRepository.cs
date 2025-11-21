using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        protected override string GetTableName() => "USERS";
        protected override string GetPrimaryKey() => "UserId";

        protected override string GetInsertQuery() => @"
            INSERT INTO USERS (Username, PasswordHash, FullName, Email, Phone, Avatar, RoleId, IsActive, Theme)
            VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @Avatar, @RoleId, @IsActive, @Theme);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE USERS SET
                FullName = @FullName, Email = @Email, Phone = @Phone,
                Avatar = @Avatar, RoleId = @RoleId, IsActive = @IsActive,
                Theme = @Theme, UpdatedAt = GETDATE()
            WHERE UserId = @UserId";

        /// <summary>
        /// Đăng nhập - Kiểm tra username và password
        /// </summary>
        public async Task<User?> LoginAsync(string username, string passwordHash)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT u.*, r.RoleName
                FROM USERS u
                JOIN ROLES r ON u.RoleId = r.RoleId
                WHERE u.Username = @Username AND u.PasswordHash = @PasswordHash AND u.IsActive = 1";

            var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { Username = username, PasswordHash = passwordHash });

            if (user != null)
            {
                // Cập nhật LastLogin
                await conn.ExecuteAsync("UPDATE USERS SET LastLogin = GETDATE() WHERE UserId = @UserId",
                    new { user.UserId });
            }

            return user;
        }

        /// <summary>
        /// Lấy user theo username
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT u.*, r.RoleName
                FROM USERS u
                JOIN ROLES r ON u.RoleId = r.RoleId
                WHERE u.Username = @Username";
            return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
        }

        /// <summary>
        /// Lấy danh sách user với role
        /// </summary>
        public async Task<IEnumerable<User>> GetAllWithRoleAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT u.*, r.RoleName
                FROM USERS u
                JOIN ROLES r ON u.RoleId = r.RoleId
                ORDER BY u.RoleId, u.FullName";
            return await conn.QueryAsync<User>(sql);
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string newPasswordHash)
        {
            using var conn = GetConnection();
            var sql = "UPDATE USERS SET PasswordHash = @PasswordHash, UpdatedAt = GETDATE() WHERE UserId = @UserId";
            var result = await conn.ExecuteAsync(sql, new { UserId = userId, PasswordHash = newPasswordHash });
            return result > 0;
        }

        /// <summary>
        /// Cập nhật theme
        /// </summary>
        public async Task<bool> UpdateThemeAsync(int userId, string theme)
        {
            using var conn = GetConnection();
            var sql = "UPDATE USERS SET Theme = @Theme WHERE UserId = @UserId";
            var result = await conn.ExecuteAsync(sql, new { UserId = userId, Theme = theme });
            return result > 0;
        }

        /// <summary>
        /// Khóa/Mở khóa user
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int userId)
        {
            using var conn = GetConnection();
            var sql = "UPDATE USERS SET IsActive = ~IsActive, UpdatedAt = GETDATE() WHERE UserId = @UserId";
            var result = await conn.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }

        /// <summary>
        /// Kiểm tra username đã tồn tại chưa
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM USERS WHERE Username = @Username";
            if (excludeUserId.HasValue)
                sql += " AND UserId != @ExcludeId";

            return await conn.ExecuteScalarAsync<int>(sql,
                new { Username = username, ExcludeId = excludeUserId }) > 0;
        }

        /// <summary>
        /// Lấy tất cả roles
        /// </summary>
        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<Role>("SELECT * FROM ROLES ORDER BY RoleId");
        }
    }
}
