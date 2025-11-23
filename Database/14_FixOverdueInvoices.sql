-- =====================================================
-- FIX: Cập nhật lại hóa đơn tháng hiện tại bị sai NgayHetHan
-- =====================================================
USE QUANLYNHATRO;
GO

PRINT N'═══════════════════════════════════════════════════════';
PRINT N'FIX #14: Sửa hóa đơn tháng hiện tại bị quá hạn sai';
PRINT N'═══════════════════════════════════════════════════════';
GO

-- Cập nhật NgayHetHan và TrangThai cho hóa đơn tháng hiện tại (11/2025)
-- Nếu hóa đơn được tạo trong tháng này nhưng NgayHetHan đã quá
UPDATE HOADON
SET NgayHetHan = DATEADD(DAY, 10, GETDATE()),
    TrangThai = N'ChuaThanhToan',
    UpdatedAt = GETDATE()
WHERE MONTH(ThangNam) = MONTH(GETDATE())
  AND YEAR(ThangNam) = YEAR(GETDATE())
  AND TrangThai IN (N'QuaHan', N'ChuaThanhToan')
  AND ConNo > 0
  AND NgayHetHan < GETDATE();

DECLARE @UpdatedCount INT = @@ROWCOUNT;

IF @UpdatedCount > 0
BEGIN
    PRINT N'✅ Đã cập nhật ' + CAST(@UpdatedCount AS NVARCHAR(10)) + N' hóa đơn:';
    PRINT N'   - NgayHetHan: ' + FORMAT(DATEADD(DAY, 10, GETDATE()), 'dd/MM/yyyy');
    PRINT N'   - TrangThai: ChuaThanhToan';
END
ELSE
BEGIN
    PRINT N'ℹ️ Không có hóa đơn nào cần cập nhật.';
END
GO

PRINT N'';
PRINT N'✅ Hoàn thành fix hóa đơn quá hạn!';
PRINT N'═══════════════════════════════════════════════════════';
GO
