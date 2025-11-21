using Microsoft.Data.SqlClient;
using System.Data;

namespace QuanLyNhaTro.DAL
{
    /// <summary>
    /// Helper class để quản lý kết nối database
    /// </summary>
    public static class DatabaseHelper
    {
        private static string _connectionString = string.Empty;

        /// <summary>
        /// Connection string hiện tại
        /// </summary>
        public static string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

        /// <summary>
        /// Khởi tạo connection string từ các tham số
        /// </summary>
        public static void Initialize(string server, string database, bool integratedSecurity = true,
            string? userId = null, string? password = null)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = integratedSecurity,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            if (!integratedSecurity && !string.IsNullOrEmpty(userId))
            {
                builder.UserID = userId;
                builder.Password = password;
            }

            _connectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Khởi tạo với connection string có sẵn
        /// </summary>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tạo mới một SqlConnection
        /// </summary>
        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Mở connection và trả về
        /// </summary>
        public static SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Test kết nối database
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Thực thi stored procedure không trả về dữ liệu
        /// </summary>
        public static int ExecuteNonQuery(string storedProcedure, SqlParameter[]? parameters = null)
        {
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Thực thi câu SQL không trả về dữ liệu
        /// </summary>
        public static int ExecuteNonQueryText(string sql, SqlParameter[]? parameters = null)
        {
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Lấy giá trị đơn lẻ
        /// </summary>
        public static object? ExecuteScalar(string sql, SqlParameter[]? parameters = null)
        {
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Lấy DataTable từ SQL
        /// </summary>
        public static DataTable ExecuteQuery(string sql, SqlParameter[]? parameters = null)
        {
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            var dt = new DataTable();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Lấy DataTable từ Stored Procedure
        /// </summary>
        public static DataTable ExecuteStoredProcedure(string storedProcedure, SqlParameter[]? parameters = null)
        {
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            var dt = new DataTable();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }
    }
}
