using QuanLyNhaTro.BLL.Helpers;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;

namespace QuanLyNhaTro.BLL.Services
{
    /// <summary>
    /// Service xử lý Ghi nhận hư hỏng tài sản
    /// FIX Issue 4.1: Ghi nhận + phê duyệt trước khi khấu trừ tiền cọc
    /// </summary>
    public class DamageReportService
    {
        private readonly DamageReportRepository _repo = new();
        private readonly ActivityLogRepository _logRepo = new();
        private readonly NotificationRepository _notiRepo = new();

        /// <summary>
        /// Lấy danh sách ghi nhận chờ phê duyệt
        /// </summary>
        public async Task<IEnumerable<DamageReport>> GetPendingAsync()
        {
            return await _repo.GetPendingAsync();
        }

        /// <summary>
        /// Lấy danh sách ghi nhận theo hợp đồng
        /// </summary>
        public async Task<IEnumerable<DamageReport>> GetByHopDongAsync(int hopDongId)
        {
            return await _repo.GetByHopDongAsync(hopDongId);
        }

        /// <summary>
        /// Ghi nhận hư hỏng tài sản
        /// </summary>
        public async Task<(bool Success, string Message, int DamageId)> CreateAsync(DamageReport report)
        {
            // Validation
            if (!ValidationHelper.IsPositive(report.GiaTriHuHong))
                return (false, "Giá trị hư hỏng phải lớn hơn 0!", 0);

            if (string.IsNullOrWhiteSpace(report.MoTa))
                return (false, "Vui lòng nhập mô tả hư hỏng!", 0);

            report.NgayGhiNhan = DateTime.Now;
            report.TrangThai = "PendingApproval";

            var result = await _repo.CreateAsync(report);

            if (result.Success)
            {
                await _logRepo.LogAsync(report.NguoiGhiNhan, "DAMAGE_REPORT", result.DamageId.ToString(), "INSERT",
                    duLieuMoi: report, moTa: $"Ghi nhận hư hỏng tài sản. Giá trị: {report.GiaTriHuHong:N0} VND");
            }

            return result;
        }

        /// <summary>
        /// Phê duyệt ghi nhận hư hỏng
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveAsync(int damageId, int adminId)
        {
            var report = await _repo.GetByIdAsync(damageId);
            if (report == null)
                return (false, "Ghi nhận hư hỏng không tồn tại!");

            if (report.TrangThai != "PendingApproval")
                return (false, "Ghi nhận hư hỏng không ở trạng thái chờ phê duyệt!");

            var result = await _repo.ApproveAsync(damageId, adminId, true);

            if (result.Success)
            {
                await _logRepo.LogAsync(adminId, "DAMAGE_REPORT", damageId.ToString(), "UPDATE",
                    duLieuCu: report, moTa: $"Phê duyệt ghi nhận hư hỏng. Khấu trừ: {report.GiaTriHuHong:N0} VND");
            }

            return result;
        }

        /// <summary>
        /// Từ chối ghi nhận hư hỏng
        /// </summary>
        public async Task<(bool Success, string Message)> RejectAsync(int damageId, int adminId, string lyDoTuChoi)
        {
            var report = await _repo.GetByIdAsync(damageId);
            if (report == null)
                return (false, "Ghi nhận hư hỏng không tồn tại!");

            if (report.TrangThai != "PendingApproval")
                return (false, "Ghi nhận hư hỏng không ở trạng thái chờ phê duyệt!");

            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
                return (false, "Vui lòng nhập lý do từ chối!");

            var result = await _repo.ApproveAsync(damageId, adminId, false, lyDoTuChoi);

            if (result.Success)
            {
                await _logRepo.LogAsync(adminId, "DAMAGE_REPORT", damageId.ToString(), "UPDATE",
                    duLieuCu: report, moTa: $"Từ chối ghi nhận hư hỏng. Lý do: {lyDoTuChoi}");
            }

            return result;
        }

        /// <summary>
        /// Tính tổng giá trị hư hỏng được phê duyệt (dùng khi thanh lý)
        /// </summary>
        public async Task<decimal> GetTotalApprovedDamageByContractAsync(int hopDongId)
        {
            return await _repo.GetTotalApprovedDamageByContractAsync(hopDongId);
        }

        /// <summary>
        /// Lấy ghi nhận theo ID
        /// </summary>
        public async Task<DamageReport?> GetByIdAsync(int damageId)
        {
            return await _repo.GetByIdAsync(damageId);
        }
    }
}
