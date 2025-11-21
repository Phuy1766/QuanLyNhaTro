using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    public class BaoTriService
    {
        private readonly BaoTriRepository _repo = new();
        private readonly NotificationRepository _notiRepo = new();
        private readonly ActivityLogRepository _logRepo = new();

        public async Task<IEnumerable<BaoTriTicket>> GetAllAsync(string? trangThai = null)
        {
            return await _repo.GetAllWithDetailsAsync(trangThai);
        }

        public async Task<IEnumerable<BaoTriTicket>> GetNewTicketsAsync()
        {
            return await _repo.GetNewTicketsAsync();
        }

        public async Task<BaoTriTicket?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<int> CountNewAsync()
        {
            return await _repo.CountByStatusAsync("Mới");
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(BaoTriTicket ticket)
        {
            if (ticket.PhongId <= 0)
                return (false, "Vui lòng chọn phòng!", 0);

            if (!ValidationHelper.IsNotEmpty(ticket.TieuDe))
                return (false, "Tiêu đề không được để trống!", 0);

            ticket.MaTicket = await _repo.GenerateMaTicketAsync();
            ticket.TrangThai = "Mới";

            var id = await _repo.InsertAsync(ticket);

            if (id > 0)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BAOTRI_TICKET", ticket.MaTicket, "INSERT",
                    duLieuMoi: ticket, moTa: $"Tạo yêu cầu bảo trì {ticket.MaTicket}");

                await _notiRepo.AddAsync(new Notification
                {
                    LoaiThongBao = "BaoTriMoi",
                    TieuDe = "Yêu cầu bảo trì mới",
                    NoiDung = $"Có yêu cầu bảo trì mới: {ticket.TieuDe}"
                });
            }

            return (id > 0, id > 0 ? $"Tạo yêu cầu {ticket.MaTicket} thành công!" : "Tạo yêu cầu thất bại!", id);
        }

        public async Task<(bool Success, string Message)> ProcessAsync(int ticketId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId);
            if (ticket == null)
                return (false, "Không tìm thấy yêu cầu!");

            if (ticket.TrangThai != "Mới")
                return (false, "Yêu cầu đã được xử lý!");

            var result = await _repo.ProcessTicketAsync(ticketId, AuthService.CurrentUser?.UserId ?? 0);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BAOTRI_TICKET", ticket.MaTicket, "PROCESS",
                    moTa: $"Bắt đầu xử lý yêu cầu {ticket.MaTicket}");
            }

            return (result, result ? "Đã tiếp nhận yêu cầu!" : "Thao tác thất bại!");
        }

        public async Task<(bool Success, string Message)> CompleteAsync(int ticketId, string ketQuaXuLy, decimal chiPhi)
        {
            var ticket = await _repo.GetByIdAsync(ticketId);
            if (ticket == null)
                return (false, "Không tìm thấy yêu cầu!");

            if (ticket.TrangThai == "Hoàn thành")
                return (false, "Yêu cầu đã hoàn thành!");

            var result = await _repo.CompleteTicketAsync(ticketId, ketQuaXuLy, chiPhi);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BAOTRI_TICKET", ticket.MaTicket, "COMPLETE",
                    moTa: $"Hoàn thành yêu cầu {ticket.MaTicket}, chi phí: {chiPhi:N0}");
            }

            return (result, result ? "Hoàn thành yêu cầu!" : "Thao tác thất bại!");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int ticketId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId);
            if (ticket == null)
                return (false, "Không tìm thấy yêu cầu!");

            var result = await _repo.DeleteAsync(ticketId);

            if (result)
            {
                await _logRepo.LogAsync(AuthService.CurrentUser?.UserId, "BAOTRI_TICKET", ticket.MaTicket, "DELETE",
                    duLieuCu: ticket, moTa: $"Xóa yêu cầu {ticket.MaTicket}");
            }

            return (result, result ? "Xóa thành công!" : "Xóa thất bại!");
        }
    }
}
