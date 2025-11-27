using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace QuanLyNhaTro.DAL.Repositories
{
    /// <summary>
    /// Base Repository cung cấp các phương thức CRUD cơ bản
    /// </summary>
    public abstract class BaseRepository<T> where T : class
    {
        protected SqlConnection GetConnection() => DatabaseHelper.CreateConnection();

        /// <summary>
        /// Check if a column exists in the table
        /// </summary>
        protected async Task<bool> ColumnExistsAsync(string columnName)
        {
            using var conn = GetConnection();
            var sql = @"SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID(@TableName) AND name = @ColumnName";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { TableName = "dbo." + GetTableName(), ColumnName = columnName });
            return count > 0;
        }

        /// <summary>
        /// Lấy tất cả records
        /// </summary>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            using var conn = GetConnection();
            var hasIsActive = await ColumnExistsAsync("IsActive");
            var sql = hasIsActive ? $"SELECT * FROM {GetTableName()} WHERE IsActive = 1" : $"SELECT * FROM {GetTableName()}";
            return await conn.QueryAsync<T>(sql);
        }

        /// <summary>
        /// Lấy record theo ID
        /// </summary>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            using var conn = GetConnection();
            var hasIsActive = await ColumnExistsAsync("IsActive");
            var sql = hasIsActive ? $"SELECT * FROM {GetTableName()} WHERE {GetPrimaryKey()} = @Id AND IsActive = 1" : $"SELECT * FROM {GetTableName()} WHERE {GetPrimaryKey()} = @Id";
            return await conn.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        /// <summary>
        /// Thêm mới record
        /// </summary>
        public virtual async Task<int> InsertAsync(T entity)
        {
            using var conn = GetConnection();
            var sql = GetInsertQuery();
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        /// <summary>
        /// Cập nhật record
        /// </summary>
        public virtual async Task<bool> UpdateAsync(T entity)
        {
            using var conn = GetConnection();
            var sql = GetUpdateQuery();
            var result = await conn.ExecuteAsync(sql, entity);
            return result > 0;
        }

        /// <summary>
        /// Xóa mềm record (set IsActive = 0)
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            using var conn = GetConnection();
            // If database doesn't have IsActive column, perform hard delete
            var hasIsActive = await ColumnExistsAsync("IsActive");
            if (!hasIsActive)
                return await HardDeleteAsync(id);

            var sql = $"UPDATE {GetTableName()} SET IsActive = 0, UpdatedAt = GETDATE() WHERE {GetPrimaryKey()} = @Id";
            var result = await conn.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }

        /// <summary>
        /// Xóa cứng record
        /// </summary>
        public virtual async Task<bool> HardDeleteAsync(int id)
        {
            using var conn = GetConnection();
            var sql = $"DELETE FROM {GetTableName()} WHERE {GetPrimaryKey()} = @Id";
            var result = await conn.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }

        /// <summary>
        /// Đếm số record
        /// </summary>
        public virtual async Task<int> CountAsync()
        {
            using var conn = GetConnection();
            var hasIsActive2 = await ColumnExistsAsync("IsActive");
            var sql = hasIsActive2 ? $"SELECT COUNT(*) FROM {GetTableName()} WHERE IsActive = 1" : $"SELECT COUNT(*) FROM {GetTableName()}";
            return await conn.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        /// Kiểm tra tồn tại
        /// </summary>
        public virtual async Task<bool> ExistsAsync(int id)
        {
            using var conn = GetConnection();
            var sql = $"SELECT COUNT(1) FROM {GetTableName()} WHERE {GetPrimaryKey()} = @Id";
            return await conn.ExecuteScalarAsync<int>(sql, new { Id = id }) > 0;
        }

        // Abstract methods - các lớp con phải implement
        protected abstract string GetTableName();
        protected abstract string GetPrimaryKey();
        protected abstract string GetInsertQuery();
        protected abstract string GetUpdateQuery();
    }
}
