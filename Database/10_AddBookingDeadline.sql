/*
 * MIGRATION: Thêm NgayHetHan cho YEUCAU_THUEPHONG
 * FIX Issue 5.1: Auto-cancel yêu cầu hết hạn thanh toán
 * Date: 2025-01-23
 */
USE QuanLyNhaTro;
GO

-- =====================================================
-- Thêm cột NgayHetHan nếu chưa có
-- =====================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'YEUCAU_THUEPHONG' AND COLUMN_NAME = 'NgayHetHan'
)
BEGIN
    ALTER TABLE YEUCAU_THUEPHONG
    ADD NgayHetHan DATETIME NULL;
    
    PRINT N'✅ Đã thêm cột NgayHetHan vào YEUCAU_THUEPHONG';
END
GO

-- =====================================================
-- Thêm cột MoTaHuyBoSung để ghi nhận lý do tự động hủy
-- =====================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'YEUCAU_THUEPHONG' AND COLUMN_NAME = 'MoTaHuyBoSung'
)
BEGIN
    ALTER TABLE YEUCAU_THUEPHONG
    ADD MoTaHuyBoSung NVARCHAR(500) NULL;
    
    PRINT N'✅ Đã thêm cột MoTaHuyBoSung vào YEUCAU_THUEPHONG';
END
GO

-- =====================================================
-- STORED PROCEDURE: Tự động hủy yêu cầu hết hạn
-- Chạy hàng ngày (dùng SQL Server Agent Job)
-- =====================================================

IF OBJECT_ID('sp_AutoCancelExpiredBookingRequests', 'P') IS NOT NULL
    DROP PROCEDURE sp_AutoCancelExpiredBookingRequests;
GO

CREATE PROCEDURE sp_AutoCancelExpiredBookingRequests
    @HoursTimeout INT = 24  -- Default: 24 giờ
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CanceledCount INT = 0;

    BEGIN TRANSACTION;
    BEGIN TRY
        -- Lấy danh sách yêu cầu hết hạn
        DECLARE @YeuCauList TABLE (
            MaYeuCau INT,
            MaTenant INT,
            PhongId INT,
            NgayGui DATETIME
        );

        INSERT INTO @YeuCauList
        SELECT MaYeuCau, MaTenant, PhongId, NgayGui
        FROM YEUCAU_THUEPHONG
        WHERE TrangThai IN ('PendingPayment', 'WaitingConfirm')
          AND NgayHetHan IS NOT NULL
          AND DATEDIFF(MINUTE, NgayHetHan, GETDATE()) > 0  -- Đã quá hạn
          AND NOT EXISTS (
              SELECT 1 FROM BOOKING_PAYMENT
              WHERE MaYeuCau = YEUCAU_THUEPHONG.MaYeuCau 
              AND TrangThai IN ('Paid', 'WaitingConfirm')  -- Đã thanh toán
          );

        -- Cập nhật yêu cầu thành Canceled
        UPDATE YEUCAU_THUEPHONG
        SET TrangThai = 'Canceled',
            MoTaHuyBoSung = N'Tự động hủy vì hết hạn thanh toán (' + CAST(@HoursTimeout AS NVARCHAR(5)) + N' giờ)',
            NgayXuLy = GETDATE()
        WHERE MaYeuCau IN (SELECT MaYeuCau FROM @YeuCauList);

        SET @CanceledCount = @@ROWCOUNT;

        -- Cập nhật BOOKING_PAYMENT thành Canceled
        UPDATE BOOKING_PAYMENT
        SET TrangThai = 'Canceled',
            GhiChu = N'Tự động hủy vì hết hạn thanh toán'
        WHERE MaYeuCau IN (SELECT MaYeuCau FROM @YeuCauList)
          AND TrangThai = 'Pending';

        -- Gửi thông báo cho Tenant
        INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, DaDoc, NgayTao)
        SELECT DISTINCT yc.MaTenant,
               N'Yêu cầu thuê phòng bị hủy',
               N'Yêu cầu thuê phòng ' + p.MaPhong + N' của bạn đã bị hủy tự động vì quá hạn thanh toán. Hãy gửi yêu cầu mới nếu vẫn muốn thuê.',
               'ThuePhong',
               0,
               GETDATE()
        FROM @YeuCauList yc
        INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
        WHERE NOT EXISTS (
            SELECT 1 FROM THONGBAO
            WHERE UserId = yc.MaTenant 
            AND LoaiThongBao = 'ThuePhong'
            AND TieuDe LIKE N'%bị hủy%'
            AND CAST(NgayTao AS DATE) = CAST(GETDATE() AS DATE)
        );

        COMMIT TRANSACTION;

        SELECT @CanceledCount AS CanceledCount, 
               N'Đã hủy ' + CAST(@CanceledCount AS NVARCHAR(10)) + N' yêu cầu hết hạn' AS Message;

    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 AS CanceledCount, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

PRINT N'✅ Hoàn thành tạo sp_AutoCancelExpiredBookingRequests';
GO

-- =====================================================
-- STORED PROCEDURE: Cập nhật NgayHetHan khi tạo yêu cầu
-- (Gọi từ YeuCauThuePhongService)
-- =====================================================

IF OBJECT_ID('sp_UpdateBookingRequestWithDeadline', 'P') IS NOT NULL
    DROP PROCEDURE sp_UpdateBookingRequestWithDeadline;
GO

CREATE PROCEDURE sp_UpdateBookingRequestWithDeadline
    @MaYeuCau INT,
    @HoursDeadline INT = 24  -- Default: 24 giờ từ lúc tạo
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NgayGui DATETIME;
    SELECT @NgayGui = NgayGui FROM YEUCAU_THUEPHONG WHERE MaYeuCau = @MaYeuCau;

    IF @NgayGui IS NOT NULL
    BEGIN
        UPDATE YEUCAU_THUEPHONG
        SET NgayHetHan = DATEADD(HOUR, @HoursDeadline, @NgayGui)
        WHERE MaYeuCau = @MaYeuCau;

        SELECT 1 AS Success, 
               N'Đã thiết lập hạn thanh toán: ' + FORMAT(DATEADD(HOUR, @HoursDeadline, @NgayGui), 'dd/MM/yyyy HH:mm') AS Message;
    END
    ELSE
    BEGIN
        SELECT 0 AS Success, N'Yêu cầu không tồn tại' AS Message;
    END
END
GO

PRINT N'✅ Hoàn thành tạo sp_UpdateBookingRequestWithDeadline';
GO

-- =====================================================
-- HƯỚNG DẪN THIẾT LẬP SQL SERVER AGENT JOB
-- Chạy sp_AutoCancelExpiredBookingRequests mỗi giờ
-- =====================================================

/*
Bước 1: Mở SQL Server Management Studio
Bước 2: Kết nối tới SQL Server Agent
Bước 3: Chuột phải vào Jobs > New Job

Job Name: "AutoCancelExpiredBookingRequests"
Description: "Tự động hủy yêu cầu thuê phòng hết hạn thanh toán"

Step 1:
  Step name: "Cancel Expired"
  Type: "Transact-SQL script"
  Database: "QuanLyNhaTro"
  Command:
    EXECUTE sp_AutoCancelExpiredBookingRequests @HoursTimeout = 24;

Schedule:
  Type: Recurring
  Frequency: Daily
  Every: 1 day
  Time: 00:00:00 (Chạy hàng ngày lúc nửa đêm)
  
Hoặc: Recurring every 1 hour (Nếu muốn kiểm tra thường xuyên)
*/

PRINT N'========================================';
PRINT N'✅ Hoàn thành FIX 5.1: Timeout yêu cầu';
PRINT N'========================================';
GO
