/*
 * Migration: Fix High Priority Business Logic Issues
 * Issues Fixed:
 *   6. Phí thanh lý sớm (early termination penalty)
 *   7. Auto-cancel booking requests hết hạn
 *   10. Thêm các database constraints còn thiếu
 * Date: 2025-11-23
 */

USE QuanLyNhaTro;
GO

-- =====================================================
-- 1. THÊM CÁC CONSTRAINTS CÒN THIẾU
-- =====================================================

-- Constraint: DienTich > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PhongTro_DienTich_Positive')
BEGIN
    ALTER TABLE PHONGTRO
    ADD CONSTRAINT CHK_PhongTro_DienTich_Positive 
    CHECK (DienTich > 0);
    
    PRINT N'✅ Đã thêm constraint: DienTich > 0';
END
GO

-- Constraint: TienHoanCoc <= TienCoc
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HopDong_TienHoanCoc_LessOrEqual_TienCoc')
BEGIN
    ALTER TABLE HOPDONG
    ADD CONSTRAINT CHK_HopDong_TienHoanCoc_LessOrEqual_TienCoc 
    CHECK (TienHoanCoc IS NULL OR TienHoanCoc <= TienCoc);
    
    PRINT N'✅ Đã thêm constraint: TienHoanCoc <= TienCoc';
END
GO

-- Constraint: TienKhauTru >= 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HopDong_TienKhauTru_NonNegative')
BEGIN
    ALTER TABLE HOPDONG
    ADD CONSTRAINT CHK_HopDong_TienKhauTru_NonNegative 
    CHECK (TienKhauTru IS NULL OR TienKhauTru >= 0);
    
    PRINT N'✅ Đã thêm constraint: TienKhauTru >= 0';
END
GO

-- Constraint: CCCD unique
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_KhachThue_CCCD')
BEGIN
    ALTER TABLE KHACHTHUE
    ADD CONSTRAINT UQ_KhachThue_CCCD UNIQUE (CCCD);
    
    PRINT N'✅ Đã thêm constraint: CCCD unique';
END
GO

-- =====================================================
-- 2. THÊM CỘT PHÍ PHẠT THANH LÝ SỚM
-- =====================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'HOPDONG' AND COLUMN_NAME = 'PhiPhatThanhLySom'
)
BEGIN
    ALTER TABLE HOPDONG
    ADD PhiPhatThanhLySom DECIMAL(18,0) NULL DEFAULT 0;
    
    PRINT N'✅ Đã thêm cột PhiPhatThanhLySom vào HOPDONG';
END
GO

-- Constraint: PhiPhatThanhLySom >= 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HopDong_PhiPhatThanhLySom_NonNegative')
BEGIN
    ALTER TABLE HOPDONG
    ADD CONSTRAINT CHK_HopDong_PhiPhatThanhLySom_NonNegative 
    CHECK (PhiPhatThanhLySom IS NULL OR PhiPhatThanhLySom >= 0);
    
    PRINT N'✅ Đã thêm constraint: PhiPhatThanhLySom >= 0';
END
GO

-- =====================================================
-- 3. CẬP NHẬT STORED PROCEDURE: TÍNH PHÍ THANH LÝ SỚM
-- =====================================================

IF OBJECT_ID('sp_CalculateTerminationFees', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalculateTerminationFees;
GO

CREATE PROCEDURE sp_CalculateTerminationFees
    @HopDongId INT,
    @TienCoc DECIMAL(18,0) OUTPUT,
    @CongNoHoaDon DECIMAL(18,0) OUTPUT,
    @ChiPhiHuHong DECIMAL(18,0) OUTPUT,
    @PhiPhatThanhLySom DECIMAL(18,0) OUTPUT,
    @TongKhauTru DECIMAL(18,0) OUTPUT,
    @TienHoanCoc DECIMAL(18,0) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NgayBatDau DATE, @NgayKetThuc DATE, @GiaThue DECIMAL(18,0);
    DECLARE @KhachId INT;
    DECLARE @NgayHomNay DATE = CAST(GETDATE() AS DATE);
    DECLARE @ThangConLai INT, @ThangDaO INT;
    
    -- Lấy thông tin hợp đồng
    SELECT @NgayBatDau = NgayBatDau,
           @NgayKetThuc = NgayKetThuc,
           @GiaThue = GiaThue,
           @TienCoc = TienCoc,
           @KhachId = KhachId
    FROM HOPDONG
    WHERE HopDongId = @HopDongId AND TrangThai = N'Active';
    
    IF @KhachId IS NULL
    BEGIN
        RAISERROR(N'Hợp đồng không tồn tại hoặc không ở trạng thái Active', 16, 1);
        RETURN;
    END
    
    -- 1. Tính công nợ hóa đơn
    SELECT @CongNoHoaDon = ISNULL(SUM(ConNo), 0)
    FROM HOADON
    WHERE HopDongId = @HopDongId 
      AND TrangThai != N'DaThanhToan';
    
    -- 2. Tính chi phí hư hỏng (chỉ những cái đã được phê duyệt)
    SELECT @ChiPhiHuHong = ISNULL(SUM(GiaTriHuHong), 0)
    FROM DAMAGE_REPORT
    WHERE HopDongId = @HopDongId 
      AND TrangThai = 'Approved';
    
    -- 3. Tính phí phạt thanh lý sớm
    SET @PhiPhatThanhLySom = 0;
    
    IF @NgayHomNay < @NgayKetThuc
    BEGIN
        -- Tính số tháng còn lại
        SET @ThangConLai = DATEDIFF(MONTH, @NgayHomNay, @NgayKetThuc);
        
        -- Tính số tháng đã ở
        SET @ThangDaO = DATEDIFF(MONTH, @NgayBatDau, @NgayHomNay);
        
        -- Policy thanh lý sớm:
        -- - Nếu còn > 6 tháng: Phạt 2 tháng tiền phòng
        -- - Nếu còn 3-6 tháng: Phạt 1 tháng tiền phòng
        -- - Nếu còn < 3 tháng: Phạt 50% 1 tháng
        -- - Nếu đã ở < 1 tháng: Không hoàn cọc
        
        IF @ThangDaO < 1
        BEGIN
            -- Thanh lý trong tháng đầu: Mất toàn bộ tiền cọc
            SET @PhiPhatThanhLySom = @TienCoc;
        END
        ELSE IF @ThangConLai > 6
        BEGIN
            -- Còn > 6 tháng: Phạt 2 tháng
            SET @PhiPhatThanhLySom = @GiaThue * 2;
        END
        ELSE IF @ThangConLai BETWEEN 3 AND 6
        BEGIN
            -- Còn 3-6 tháng: Phạt 1 tháng
            SET @PhiPhatThanhLySom = @GiaThue;
        END
        ELSE IF @ThangConLai > 0
        BEGIN
            -- Còn < 3 tháng: Phạt 50%
            SET @PhiPhatThanhLySom = @GiaThue * 0.5;
        END
    END
    
    -- 4. Tính tổng và tiền hoàn cọc
    SET @TongKhauTru = @CongNoHoaDon + @ChiPhiHuHong + @PhiPhatThanhLySom;
    SET @TienHoanCoc = CASE 
        WHEN @TongKhauTru >= @TienCoc THEN 0 
        ELSE @TienCoc - @TongKhauTru 
    END;
END
GO

PRINT N'✅ Đã tạo sp_CalculateTerminationFees';
GO

-- =====================================================
-- 4. KIỂM TRA VÀ SỬA SP AUTO-CANCEL (NẾU CHƯA CÓ)
-- =====================================================

-- Kiểm tra SP có tồn tại không
IF OBJECT_ID('sp_AutoCancelExpiredBookingRequests', 'P') IS NULL
BEGIN
    PRINT N'⚠️ SP sp_AutoCancelExpiredBookingRequests chưa tồn tại. Tạo mới...';
    
    EXEC('
    CREATE PROCEDURE sp_AutoCancelExpiredBookingRequests
        @HoursTimeout INT = 24
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
                PhongId INT
            );

            INSERT INTO @YeuCauList
            SELECT MaYeuCau, MaTenant, PhongId
            FROM YEUCAU_THUEPHONG
            WHERE TrangThai IN (''PendingPayment'', ''WaitingConfirm'')
              AND NgayHetHan IS NOT NULL
              AND NgayHetHan < GETDATE();

            -- Cập nhật yêu cầu thành Canceled
            UPDATE YEUCAU_THUEPHONG
            SET TrangThai = ''Canceled'',
                MoTaHuyBoSung = N''Tự động hủy vì hết hạn thanh toán'',
                NgayXuLy = GETDATE()
            WHERE MaYeuCau IN (SELECT MaYeuCau FROM @YeuCauList);

            SET @CanceledCount = @@ROWCOUNT;

            -- Cập nhật BOOKING_PAYMENT thành Canceled
            UPDATE BOOKING_PAYMENT
            SET TrangThai = ''Canceled'',
                GhiChu = N''Tự động hủy vì hết hạn thanh toán''
            WHERE MaYeuCau IN (SELECT MaYeuCau FROM @YeuCauList)
              AND TrangThai IN (''Pending'', ''WaitingConfirm'');

            -- Gửi thông báo cho Tenant
            INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, DaDoc, NgayTao)
            SELECT DISTINCT yc.MaTenant,
                   N''Yêu cầu thuê phòng bị hủy'',
                   N''Yêu cầu thuê phòng của bạn đã bị hủy tự động vì quá hạn thanh toán.'',
                   ''ThuePhong'',
                   0,
                   GETDATE()
            FROM @YeuCauList yc;

            COMMIT TRANSACTION;

            SELECT @CanceledCount AS CanceledCount, 
                   N''Đã hủy '' + CAST(@CanceledCount AS NVARCHAR(10)) + N'' yêu cầu hết hạn'' AS Message;
        END TRY
        BEGIN CATCH
            ROLLBACK TRANSACTION;
            SELECT 0 AS CanceledCount, ERROR_MESSAGE() AS Message;
        END CATCH
    END
    ');
    
    PRINT N'✅ Đã tạo sp_AutoCancelExpiredBookingRequests';
END
ELSE
BEGIN
    PRINT N'✅ SP sp_AutoCancelExpiredBookingRequests đã tồn tại';
END
GO

-- =====================================================
-- 5. THÊM INDEX ĐỂ TỐI ƯU PERFORMANCE
-- =====================================================

-- Index cho tìm kiếm hợp đồng sắp hết hạn
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HopDong_NgayKetThuc_TrangThai')
BEGIN
    CREATE NONCLUSTERED INDEX IX_HopDong_NgayKetThuc_TrangThai 
    ON HOPDONG(NgayKetThuc, TrangThai)
    INCLUDE (HopDongId, PhongId, KhachId);
    
    PRINT N'✅ Đã tạo index: IX_HopDong_NgayKetThuc_TrangThai';
END
GO

-- Index cho tìm kiếm yêu cầu hết hạn
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_YeuCau_NgayHetHan_TrangThai')
BEGIN
    CREATE NONCLUSTERED INDEX IX_YeuCau_NgayHetHan_TrangThai 
    ON YEUCAU_THUEPHONG(NgayHetHan, TrangThai)
    INCLUDE (MaYeuCau, MaTenant, PhongId);
    
    PRINT N'✅ Đã tạo index: IX_YeuCau_NgayHetHan_TrangThai';
END
GO

-- =====================================================
-- 6. TRIGGER: TỰ ĐỘNG TÍNH ConNo TRONG HOADON
-- =====================================================

IF OBJECT_ID('trg_HoaDon_UpdateConNo', 'TR') IS NOT NULL
    DROP TRIGGER trg_HoaDon_UpdateConNo;
GO

CREATE TRIGGER trg_HoaDon_UpdateConNo
ON HOADON
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Tự động tính ConNo = TongCong - DaThanhToan
    UPDATE hd
    SET ConNo = hd.TongCong - hd.DaThanhToan,
        TrangThai = CASE 
            WHEN hd.DaThanhToan >= hd.TongCong THEN N'DaThanhToan'
            WHEN hd.NgayHetHan < GETDATE() AND hd.DaThanhToan < hd.TongCong THEN N'QuaHan'
            ELSE N'ChuaThanhToan'
        END
    FROM HOADON hd
    INNER JOIN inserted i ON hd.HoaDonId = i.HoaDonId;
END
GO

PRINT N'✅ Đã tạo trigger: trg_HoaDon_UpdateConNo';
GO

-- =====================================================
-- 7. VALIDATION: SỐ ĐIỆN THOẠI (CHECK CONSTRAINT)
-- =====================================================

-- Không thể dùng CHECK constraint với regex trong SQL Server
-- Phải validate ở application layer
-- Nhưng có thể check format cơ bản

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Users_Phone_Format')
BEGIN
    ALTER TABLE USERS
    ADD CONSTRAINT CHK_Users_Phone_Format 
    CHECK (Phone IS NULL OR LEN(Phone) BETWEEN 10 AND 15);
    
    PRINT N'✅ Đã thêm constraint: Phone length 10-15';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_KhachThue_Phone_Format')
BEGIN
    ALTER TABLE KHACHTHUE
    ADD CONSTRAINT CHK_KhachThue_Phone_Format 
    CHECK (Phone IS NULL OR LEN(Phone) BETWEEN 10 AND 15);
    
    PRINT N'✅ Đã thêm constraint: Phone length 10-15 cho KHACHTHUE';
END
GO

-- =====================================================
-- SUMMARY
-- =====================================================

PRINT N'';
PRINT N'========================================';
PRINT N'✅ HOÀN THÀNH FIX HIGH PRIORITY ISSUES';
PRINT N'========================================';
PRINT N'1. ✓ Thêm cột PhiPhatThanhLySom';
PRINT N'2. ✓ Tạo sp_CalculateTerminationFees';
PRINT N'3. ✓ Tạo/kiểm tra sp_AutoCancelExpiredBookingRequests';
PRINT N'4. ✓ Thêm constraints: DienTich, TienHoanCoc, CCCD';
PRINT N'5. ✓ Thêm indexes để optimize performance';
PRINT N'6. ✓ Tạo trigger tự động tính ConNo';
PRINT N'7. ✓ Validation phone format';
PRINT N'';
PRINT N'📝 GHI CHÚ:';
PRINT N'   - Policy phí phạt: Xem sp_CalculateTerminationFees';
PRINT N'   - Trigger ConNo tự động update';
PRINT N'   - Cần update HopDongService.cs để dùng SP mới';
PRINT N'========================================';
GO
