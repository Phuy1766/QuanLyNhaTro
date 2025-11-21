using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class BuildingRepository : BaseRepository<Building>
    {
        protected override string GetTableName() => "BUILDING";
        protected override string GetPrimaryKey() => "BuildingId";

        protected override string GetInsertQuery() => @"
            INSERT INTO BUILDING (BuildingCode, BuildingName, Address, TotalFloors, Description, IsActive)
            VALUES (@BuildingCode, @BuildingName, @Address, @TotalFloors, @Description, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE BUILDING SET
                BuildingCode = @BuildingCode, BuildingName = @BuildingName,
                Address = @Address, TotalFloors = @TotalFloors,
                Description = @Description, IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE BuildingId = @BuildingId";

        /// <summary>
        /// Lấy danh sách tòa nhà với số phòng
        /// </summary>
        public async Task<IEnumerable<Building>> GetAllWithStatsAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT b.*,
                    (SELECT COUNT(*) FROM PHONGTRO WHERE BuildingId = b.BuildingId AND IsActive = 1) AS TotalRooms,
                    (SELECT COUNT(*) FROM PHONGTRO WHERE BuildingId = b.BuildingId AND IsActive = 1 AND TrangThai = N'Trống') AS AvailableRooms
                FROM BUILDING b
                WHERE b.IsActive = 1
                ORDER BY b.BuildingCode";
            return await conn.QueryAsync<Building>(sql);
        }

        /// <summary>
        /// Kiểm tra mã tòa nhà đã tồn tại chưa
        /// </summary>
        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM BUILDING WHERE BuildingCode = @Code AND IsActive = 1";
            if (excludeId.HasValue)
                sql += " AND BuildingId != @ExcludeId";

            return await conn.ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Kiểm tra tòa nhà có phòng không (trước khi xóa)
        /// </summary>
        public async Task<bool> HasRoomsAsync(int buildingId)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(1) FROM PHONGTRO WHERE BuildingId = @BuildingId AND IsActive = 1";
            return await conn.ExecuteScalarAsync<int>(sql, new { BuildingId = buildingId }) > 0;
        }
    }
}
