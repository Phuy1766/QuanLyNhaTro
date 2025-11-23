using System.Security.Cryptography;
using System.Text;

namespace QuanLyNhaTro.BLL.Helpers
{
    /// <summary>
    /// Helper class để hash và verify password sử dụng PBKDF2
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Hash password bằng PBKDF2 với salt ngẫu nhiên
        /// Format: iterations:salt:hash (base64)
        /// </summary>
        public static string HashPassword(string password)
        {
            // Tạo salt ngẫu nhiên
            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Hash password với PBKDF2
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                Algorithm,
                KeySize
            );

            // Trả về format: iterations:salt:hash
            return $"{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verify password với hash đã lưu
        /// Hỗ trợ cả legacy SHA256 (để migration) và PBKDF2 mới
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            // Kiểm tra format mới (PBKDF2)
            var parts = storedHash.Split(':');
            if (parts.Length == 3)
            {
                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var hash = Convert.FromBase64String(parts[2]);

                var testHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations,
                    Algorithm,
                    KeySize
                );

                return CryptographicOperations.FixedTimeEquals(hash, testHash);
            }

            // Legacy SHA256 support (để migration dần)
            // TODO: Sau khi migration xong, xóa phần này
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var legacyHash = Convert.ToHexString(bytes).ToLower();
            return string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generate random password bằng cryptographically secure RNG
        /// </summary>
        public static string GenerateRandomPassword(int length = 12)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            var result = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
            
            return new string(result);
        }

        /// <summary>
        /// Kiểm tra xem hash có phải format cũ (SHA256) không
        /// </summary>
        public static bool IsLegacyHash(string hash)
        {
            return !hash.Contains(':');
        }
    }
}
