using System.Security.Cryptography;
using System.Text;

namespace QuanLyNhaTro.BLL.Helpers
{
    /// <summary>
    /// Helper class để hash và verify password
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash password bằng SHA256
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        /// <summary>
        /// Verify password
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return string.Equals(passwordHash, hash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generate random password
        /// </summary>
        public static string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
