using Dapper;
using QuanLyNhaTro.DAL.Models;

namespace QuanLyNhaTro.DAL.Repositories
{
    public class BaoTriRepository : BaseRepository<BaoTriTicket>
    {
        protected override string GetTableName() => "BAOTRI_TICKET";
        protected override string GetPrimaryKey() => "TicketId";

        protected override string GetInsertQuery() => @"
            INSERT INTO BAOTRI_TICKET (MaTicket, PhongId, KhachId, LoaiSuCo, TieuDe, MoTa, MucDoUuTien, TrangThai)
            VALUES (@MaTicket, @PhongId, @KhachId, @LoaiSuCo, @TieuDe, @MoTa, @MucDoUuTien, @TrangThai);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        protected override string GetUpdateQuery() => @"
            UPDATE BAOTRI_TICKET SET
                TieuDe = @TieuDe, MoTa = @MoTa, MucDoUuTien = @MucDoUuTien,
                TrangThai = @TrangThai, NgayXuLy = @NgayXuLy, NgayHoanThanh = @NgayHoanThanh,
                NguoiXuLy = @NguoiXuLy, ChiPhiSuaChua = @ChiPhiSuaChua, KetQuaXuLy = @KetQuaXuLy,
                UpdatedAt = GETDATE()
            WHERE TicketId = @TicketId";

        public override async Task<bool> DeleteAsync(int id)
        {
            // Hard delete for tickets
            return await HardDeleteAsync(id);
        }

        /// <summary>
        /// Lấy danh sách ticket với đầy đủ thông tin
        /// </summary>
        public async Task<IEnumerable<BaoTriTicket>> GetAllWithDetailsAsync(string? trangThai = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT bt.*, p.MaPhong, b.BuildingName, k.HoTen AS TenKhachThue, u.FullName AS TenNguoiXuLy
                FROM BAOTRI_TICKET bt
                JOIN PHONGTRO p ON bt.PhongId = p.PhongId
                JOIN BUILDING b ON p.BuildingId = b.BuildingId
                LEFT JOIN KHACHTHUE k ON bt.KhachId = k.KhachId
                LEFT JOIN USERS u ON bt.NguoiXuLy = u.UserId
                WHERE (@TrangThai IS NULL OR bt.TrangThai = @TrangThai)
                ORDER BY
                    CASE bt.MucDoUuTien
                        WHEN N'Khẩn cấp' THEN 1
                        WHEN N'Cao' THEN 2
                        WHEN N'Trung bình' THEN 3
                        ELSE 4
                    END,
                    bt.NgayTao DESC";
            return await conn.QueryAsync<BaoTriTicket>(sql, new { TrangThai = trangThai });
        }

        /// <summary>
        /// Lấy ticket mới chưa xử lý
        /// </summary>
        public async Task<IEnumerable<BaoTriTicket>> GetNewTicketsAsync()
        {
            return await GetAllWithDetailsAsync("Mới");
        }

        /// <summary>
        /// Lấy ticket theo phòng
        /// </summary>
        public async Task<IEnumerable<BaoTriTicket>> GetByPhongAsync(int phongId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT bt.*, p.MaPhong, k.HoTen AS TenKhachThue
                FROM BAOTRI_TICKET bt
                JOIN PHONGTRO p ON bt.PhongId = p.PhongId
                LEFT JOIN KHACHTHUE k ON bt.KhachId = k.KhachId
                WHERE bt.PhongId = @PhongId
                ORDER BY bt.NgayTao DESC";
            return await conn.QueryAsync<BaoTriTicket>(sql, new { PhongId = phongId });
        }

        /// <summary>
        /// Xử lý ticket
        /// </summary>
        public async Task<bool> ProcessTicketAsync(int ticketId, int nguoiXuLyId)
        {
            using var conn = GetConnection();
            var sql = @"
                UPDATE BAOTRI_TICKET SET
                    TrangThai = N'Đang xử lý', NgayXuLy = GETDATE(),
                    NguoiXuLy = @NguoiXuLyId, UpdatedAt = GETDATE()
                WHERE TicketId = @TicketId";
            return await conn.ExecuteAsync(sql, new { TicketId = ticketId, NguoiXuLyId = nguoiXuLyId }) > 0;
        }

        /// <summary>
        /// Hoàn thành ticket
        /// </summary>
        public async Task<bool> CompleteTicketAsync(int ticketId, string ketQuaXuLy, decimal chiPhi)
        {
            using var conn = GetConnection();
            var sql = @"
                UPDATE BAOTRI_TICKET SET
                    TrangThai = N'Hoàn thành', NgayHoanThanh = GETDATE(),
                    KetQuaXuLy = @KetQuaXuLy, ChiPhiSuaChua = @ChiPhi, UpdatedAt = GETDATE()
                WHERE TicketId = @TicketId";
            return await conn.ExecuteAsync(sql,
                new { TicketId = ticketId, KetQuaXuLy = ketQuaXuLy, ChiPhi = chiPhi }) > 0;
        }

        /// <summary>
        /// Sinh mã ticket tự động
        /// </summary>
        public async Task<string> GenerateMaTicketAsync()
        {
            using var conn = GetConnection();
            var prefix = $"TK{DateTime.Now:yyyyMMdd}";
            var sql = @"
                SELECT TOP 1 MaTicket FROM BAOTRI_TICKET
                WHERE MaTicket LIKE @Prefix + '%'
                ORDER BY MaTicket DESC";
            var lastCode = await conn.QueryFirstOrDefaultAsync<string>(sql, new { Prefix = prefix });

            if (string.IsNullOrEmpty(lastCode))
                return $"{prefix}001";

            var num = int.Parse(lastCode.Substring(10)) + 1;
            return $"{prefix}{num:D3}";
        }

        /// <summary>
        /// Đếm ticket theo trạng thái
        /// </summary>
        public async Task<int> CountByStatusAsync(string trangThai)
        {
            using var conn = GetConnection();
            var sql = "SELECT COUNT(*) FROM BAOTRI_TICKET WHERE TrangThai = @TrangThai";
            return await conn.ExecuteScalarAsync<int>(sql, new { TrangThai = trangThai });
        }

        /// <summary>
        /// Lấy ticket theo khách thuê (cho Tenant)
        /// </summary>
        public async Task<IEnumerable<BaoTriTicket>> GetByTenantAsync(int khachId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT bt.*, p.MaPhong, u.FullName AS TenNguoiXuLy
                FROM BAOTRI_TICKET bt
                JOIN PHONGTRO p ON bt.PhongId = p.PhongId
                LEFT JOIN USERS u ON bt.NguoiXuLy = u.UserId
                WHERE bt.KhachId = @KhachId
                ORDER BY bt.NgayTao DESC";
            return await conn.QueryAsync<BaoTriTicket>(sql, new { KhachId = khachId });
        }

        /// <summary>
        /// Thêm ticket từ Tenant
        /// </summary>
        public async Task<int> AddAsync(BaoTriTicket ticket)
        {
            using var conn = GetConnection();
            // Lấy PhongId từ MaPhong
            var phongId = await conn.ExecuteScalarAsync<int?>(
                "SELECT PhongId FROM PHONGTRO WHERE MaPhong = @MaPhong",
                new { ticket.MaPhong });

            if (phongId == null) throw new Exception("Không tìm thấy phòng");

            var maTicket = await GenerateMaTicketAsync();
            var sql = @"
                INSERT INTO BAOTRI_TICKET (MaTicket, PhongId, KhachId, TieuDe, MoTa, MucDoUuTien, TrangThai, LoaiSuCo)
                VALUES (@MaTicket, @PhongId, @KhachId, @TieuDe, @MoTa, @DoUuTien, @TrangThai, @LoaiSuCo);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                MaTicket = maTicket,
                PhongId = phongId,
                ticket.KhachId,
                ticket.TieuDe,
                ticket.MoTa,
                ticket.DoUuTien,
                ticket.TrangThai,
                ticket.LoaiSuCo
            });
        }
    }
}
