using Dapper;
using Microsoft.Data.SqlClient;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class CauHinhRepository
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        /// <summary>
        /// Lấy tất cả cấu hình
        /// </summary>
        public async Task<IEnumerable<CauHinh>> GetAllAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<CauHinh>("SELECT * FROM CAUHINH ORDER BY TenCauHinh");
        }

        /// <summary>
        /// Lấy giá trị cấu hình theo mã
        /// </summary>
        public async Task<string?> GetValueAsync(string maCauHinh)
        {
            using var conn = GetConnection();
            var sql = "SELECT GiaTri FROM CAUHINH WHERE MaCauHinh = @MaCauHinh";
            return await conn.QueryFirstOrDefaultAsync<string>(sql, new { MaCauHinh = maCauHinh });
        }

        /// <summary>
        /// Lấy giá trị cấu hình dạng số
        /// </summary>
        public async Task<decimal> GetDecimalValueAsync(string maCauHinh, decimal defaultValue = 0)
        {
            var value = await GetValueAsync(maCauHinh);
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Lấy giá trị cấu hình dạng int
        /// </summary>
        public async Task<int> GetIntValueAsync(string maCauHinh, int defaultValue = 0)
        {
            var value = await GetValueAsync(maCauHinh);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Cập nhật cấu hình
        /// </summary>
        public async Task<bool> UpdateAsync(string maCauHinh, string giaTri)
        {
            using var conn = GetConnection();
            var sql = "UPDATE CAUHINH SET GiaTri = @GiaTri, UpdatedAt = GETDATE() WHERE MaCauHinh = @MaCauHinh";
            return await conn.ExecuteAsync(sql, new { MaCauHinh = maCauHinh, GiaTri = giaTri }) > 0;
        }

        /// <summary>
        /// Cập nhật nhiều cấu hình
        /// </summary>
        public async Task UpdateManyAsync(Dictionary<string, string> configs)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                foreach (var kv in configs)
                {
                    await conn.ExecuteAsync(
                        "UPDATE CAUHINH SET GiaTri = @GiaTri, UpdatedAt = GETDATE() WHERE MaCauHinh = @MaCauHinh",
                        new { MaCauHinh = kv.Key, GiaTri = kv.Value }, trans);
                }
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Lấy cấu hình SMTP
        /// </summary>
        public async Task<SmtpConfig> GetSmtpConfigAsync()
        {
            var all = await GetAllAsync();
            var dict = all.ToDictionary(x => x.MaCauHinh, x => x.GiaTri ?? "");

            return new SmtpConfig
            {
                Host = dict.GetValueOrDefault("SMTP_HOST", "smtp.gmail.com"),
                Port = int.TryParse(dict.GetValueOrDefault("SMTP_PORT", "587"), out var port) ? port : 587,
                Email = dict.GetValueOrDefault("SMTP_EMAIL", ""),
                Password = dict.GetValueOrDefault("SMTP_PASSWORD", "")
            };
        }
    }

    public class SmtpConfig
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
