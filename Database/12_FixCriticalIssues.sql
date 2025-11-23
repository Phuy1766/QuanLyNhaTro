/*
 * Migration: Fix Critical Business Logic Issues
 * Issues Fixed:
 *   1. Auto-expire hợp đồng hết hạn
 *   2. Kiểm tra số người khi duyệt yêu cầu
 *   3. Kiểm tra hợp đồng chồng chéo
 *   4. Lưu giá phòng khi gửi yêu cầu
 * Date: 2025-11-23
 */

USE QuanLyNhaTro;
GO

-- =====================================================
-- 1. THÊM CỘT GiaPhongKhiGui VÀO YEUCAU_THUEPHONG
-- =====================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'YEUCAU_THUEPHONG' AND COLUMN_NAME = 'GiaPhongKhiGui'
)
BEGIN
    ALTER TABLE YEUCAU_THUEPHONG
    ADD GiaPhongKhiGui DECIMAL(18,0) NULL;
    
    PRINT N'✅ Đã thêm cột GiaPhongKhiGui vào YEUCAU_THUEPHONG';
END
GO

-- =====================================================
-- 2. STORED PROCEDURE: TỰ ĐỘNG EXPIRE HỢP ĐỒNG HẾT HẠN
-- =====================================================

IF OBJECT_ID('sp_AutoExpireContracts', 'P') IS NOT NULL
    DROP PROCEDURE sp_AutoExpireContracts;
GO

CREATE PROCEDURE sp_AutoExpireContracts
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ExpiredCount INT = 0;
    DECLARE @UpdatedRooms INT = 0;

    BEGIN TRANSACTION;
    BEGIN TRY
        -- Lấy danh sách hợp đồng hết hạn
        DECLARE @ExpiredContracts TABLE (
            HopDongId INT,
            MaHopDong NVARCHAR(20),
            PhongId INT,
            MaPhong NVARCHAR(20),
            KhachId INT,
            TenKhach NVARCHAR(100)
        );

        INSERT INTO @ExpiredContracts
        SELECT hd.HopDongId, hd.MaHopDong, hd.PhongId, p.MaPhong, hd.KhachId, k.HoTen
        FROM HOPDONG hd
        INNER JOIN PHONGTRO p ON hd.PhongId = p.PhongId
        INNER JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
        WHERE hd.TrangThai = N'Active'
          AND hd.NgayKetThuc < CAST(GETDATE() AS DATE);

        -- Cập nhật trạng thái hợp đồng
        UPDATE HOPDONG
        SET TrangThai = N'Expired',
            UpdatedAt = GETDATE()
        WHERE HopDongId IN (SELECT HopDongId FROM @ExpiredContracts);

        SET @ExpiredCount = @@ROWCOUNT;

        -- Cập nhật trạng thái phòng về Trống
        -- CHỈ nếu không có hợp đồng Active khác cho phòng đó
        UPDATE PHONGTRO
        SET TrangThai = N'Trống',
            UpdatedAt = GETDATE()
        WHERE PhongId IN (SELECT PhongId FROM @ExpiredContracts)
          AND NOT EXISTS (
              SELECT 1 FROM HOPDONG
              WHERE PhongId = PHONGTRO.PhongId
                AND TrangThai = N'Active'
          );

        SET @UpdatedRooms = @@ROWCOUNT;

        -- Gửi thông báo cho admin
        IF @ExpiredCount > 0
        BEGIN
            INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, DaDoc, NgayTao)
            SELECT u.UserId,
                   N'Hợp đồng hết hạn',
                   N'Có ' + CAST(@ExpiredCount AS NVARCHAR(10)) + N' hợp đồng đã hết hạn và được chuyển sang trạng thái Expired.',
                   N'HeThong',
                   0,
                   GETDATE()
            FROM USERS u
            INNER JOIN ROLES r ON u.RoleId = r.RoleId
            WHERE r.RoleName IN ('Admin', 'Manager');
        END

        COMMIT TRANSACTION;

        SELECT @ExpiredCount AS ExpiredCount, 
               @UpdatedRooms AS UpdatedRooms,
               N'Đã expire ' + CAST(@ExpiredCount AS NVARCHAR(10)) + N' hợp đồng, cập nhật ' + CAST(@UpdatedRooms AS NVARCHAR(10)) + N' phòng' AS Message;

    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 AS ExpiredCount, 0 AS UpdatedRooms, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

PRINT N'✅ Đã tạo sp_AutoExpireContracts';
GO

-- =====================================================
-- 3. CẬP NHẬT sp_ApproveBookingRequest - THÊM VALIDATIONS
-- =====================================================

IF OBJECT_ID('sp_ApproveBookingRequest', 'P') IS NOT NULL
    DROP PROCEDURE sp_ApproveBookingRequest;
GO

CREATE PROCEDURE sp_ApproveBookingRequest
    @MaYeuCau INT,
    @NguoiXuLy INT,
    @MaHopDong NVARCHAR(20) = NULL,
    @NgayKetThuc DATE,
    @TienCoc DECIMAL(18,2),
    @GhiChu NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @PhongId INT, @MaTenant INT, @NgayBatDau DATE, @GiaPhong DECIMAL(18,0);
        DECLARE @ExistingHopDong NVARCHAR(20);
        DECLARE @SoNguoi INT, @SoNguoiToiDa INT;

        -- Lock yêu cầu để tránh race condition
        SELECT @PhongId = PhongId, 
               @MaTenant = MaTenant, 
               @NgayBatDau = NgayBatDauMongMuon,
               @SoNguoi = SoNguoi,
               @GiaPhong = ISNULL(GiaPhongKhiGui, 0)
        FROM YEUCAU_THUEPHONG WITH (UPDLOCK)
        WHERE MaYeuCau = @MaYeuCau
          AND TrangThai IN ('Pending', 'PendingApprove');

        IF @PhongId IS NULL
        BEGIN
            RAISERROR(N'Yêu cầu không tồn tại hoặc đã được xử lý', 16, 1);
            ROLLBACK;
            RETURN;
        END

        -- ✅ FIX #3: Kiểm tra số người không vượt quá giới hạn
        SELECT @SoNguoiToiDa = SoNguoiToiDa 
        FROM PHONGTRO 
        WHERE PhongId = @PhongId;

        IF @SoNguoi > @SoNguoiToiDa
        BEGIN
            RAISERROR(N'Số người đăng ký (%d) vượt quá giới hạn phòng (%d)', 16, 1, @SoNguoi, @SoNguoiToiDa);
            ROLLBACK;
            RETURN;
        END

        -- ✅ FIX #5: Kiểm tra hợp đồng chồng chéo (overlap)
        IF EXISTS (
            SELECT 1 FROM HOPDONG
            WHERE PhongId = @PhongId
              AND TrangThai = N'Active'
              AND (
                  -- HĐ cũ chứa ngày bắt đầu mới
                  (@NgayBatDau BETWEEN NgayBatDau AND NgayKetThuc)
                  OR
                  -- HĐ cũ chứa ngày kết thúc mới
                  (@NgayKetThuc BETWEEN NgayBatDau AND NgayKetThuc)
                  OR
                  -- HĐ mới bao trùm HĐ cũ
                  (NgayBatDau BETWEEN @NgayBatDau AND @NgayKetThuc)
              )
        )
        BEGIN
            RAISERROR(N'Phòng đã có hợp đồng trong khoảng thời gian này', 16, 1);
            ROLLBACK;
            RETURN;
        END

        -- Kiểm tra nếu đã có hợp đồng với cùng ngày bắt đầu (tránh duplicate)
        SELECT @ExistingHopDong = MaHopDong 
        FROM HOPDONG 
        WHERE PhongId = @PhongId 
          AND NgayBatDau = @NgayBatDau
          AND TrangThai = N'Active';

        IF @ExistingHopDong IS NOT NULL
        BEGIN
            -- Hợp đồng đã tồn tại, chỉ update yêu cầu
            UPDATE YEUCAU_THUEPHONG
            SET TrangThai = N'Approved',
                NgayXuLy = GETDATE(),
                NguoiXuLy = @NguoiXuLy
            WHERE MaYeuCau = @MaYeuCau;
            
            COMMIT TRANSACTION;
            SELECT 1 AS Success, N'Duyệt yêu cầu thành công (hợp đồng ' + @ExistingHopDong + N' đã tồn tại)' AS Message;
            RETURN;
        END

        -- Kiểm tra phòng còn trống (tại thời điểm hiện tại)
        IF NOT EXISTS (SELECT 1 FROM PHONGTRO WITH (UPDLOCK) WHERE PhongId = @PhongId AND TrangThai = N'Trống')
        BEGIN
            RAISERROR(N'Phòng đã được thuê hoặc đang bảo trì', 16, 1);
            ROLLBACK;
            RETURN;
        END

        -- ✅ FIX #8: Nếu không có giá lưu, lấy từ PHONGTRO
        IF @GiaPhong = 0 OR @GiaPhong IS NULL
        BEGIN
            SELECT @GiaPhong = GiaThue FROM PHONGTRO WHERE PhongId = @PhongId;
        END

        -- Lấy KhachId từ UserId
        DECLARE @KhachId INT;
        SELECT @KhachId = KhachId FROM KHACHTHUE WHERE UserId = @MaTenant;

        IF @KhachId IS NULL
        BEGIN
            RAISERROR(N'Khách hàng không tồn tại', 16, 1);
            ROLLBACK;
            RETURN;
        END

        -- Nếu MaHopDong NULL, tự động generate
        IF @MaHopDong IS NULL OR @MaHopDong = ''
        BEGIN
            SET @MaHopDong = 'HD' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + CAST(@PhongId AS NVARCHAR(10));
        END

        -- Update trạng thái yêu cầu
        UPDATE YEUCAU_THUEPHONG
        SET TrangThai = N'Approved',
            NgayXuLy = GETDATE(),
            NguoiXuLy = @NguoiXuLy
        WHERE MaYeuCau = @MaYeuCau;

        -- Cập nhật trạng thái phòng
        UPDATE PHONGTRO
        SET TrangThai = N'Đang thuê',
            UpdatedAt = GETDATE()
        WHERE PhongId = @PhongId;

        -- ✅ FIX #4: Tạo hợp đồng với giá đã lưu
        INSERT INTO HOPDONG (MaHopDong, PhongId, KhachId, NgayBatDau, NgayKetThuc, GiaThue, TienCoc, GhiChu, TrangThai, CreatedBy, CreatedAt)
        VALUES (@MaHopDong, @PhongId, @KhachId, @NgayBatDau, @NgayKetThuc, @GiaPhong, @TienCoc, @GhiChu, N'Active', @NguoiXuLy, GETDATE());

        -- Ghi log
        BEGIN TRY
            INSERT INTO ACTIVITY_LOG (UserId, TenBang, MaBanGhi, HanhDong, DuLieuCu, DuLieuMoi, MoTa, NgayThucHien)
            VALUES (@NguoiXuLy, N'YEUCAU_THUEPHONG', CAST(@MaYeuCau AS NVARCHAR(50)), N'APPROVE', 
                    N'TrangThai=Pending', N'TrangThai=Approved', N'Duyệt yêu cầu thuê phòng', GETDATE());
        END TRY
        BEGIN CATCH
            PRINT N'⚠ Lỗi ghi log: ' + ERROR_MESSAGE();
        END CATCH

        -- Thông báo cho tenant
        BEGIN TRY
            INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, DaDoc, NgayTao)
            VALUES (@MaTenant, N'Yêu cầu thuê phòng được phê duyệt', 
                    N'Yêu cầu thuê phòng của bạn đã được phê duyệt. Hợp đồng số: ' + @MaHopDong,
                    N'ThuePhong', 0, GETDATE());
        END TRY
        BEGIN CATCH
            PRINT N'⚠ Lỗi gửi thông báo: ' + ERROR_MESSAGE();
        END CATCH

        COMMIT TRANSACTION;
        
        SELECT 1 AS Success, N'Duyệt yêu cầu thành công' AS Message;

    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
        SELECT 0 AS Success, @ErrorMessage AS Message;
    END CATCH
END;
GO

PRINT N'✅ Đã cập nhật sp_ApproveBookingRequest với validations';
GO

-- =====================================================
-- 4. CẬP NHẬT sp_CreateBookingWithPayment - LƯU GIÁ PHÒNG
-- =====================================================

IF OBJECT_ID('sp_CreateBookingWithPayment', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreateBookingWithPayment;
GO

CREATE PROCEDURE sp_CreateBookingWithPayment
    @PhongId INT,
    @MaTenant INT,
    @NgayBatDauMongMuon DATE,
    @SoNguoi INT,
    @GhiChu NVARCHAR(500) = NULL,
    @BankConfigId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @MaYeuCau INT, @GiaPhong DECIMAL(18,0), @MaPhong NVARCHAR(20);
        DECLARE @DepositMonths INT, @SoTienCoc DECIMAL(18,0), @NoiDungCK NVARCHAR(100);
        DECLARE @TransferTemplate NVARCHAR(200);

        -- Kiểm tra phòng còn trống
        SELECT @GiaPhong = GiaThue, @MaPhong = MaPhong
        FROM PHONGTRO WHERE PhongId = @PhongId AND TrangThai = N'Trống';

        IF @GiaPhong IS NULL
        BEGIN
            SELECT 0 AS Success, N'Phòng không tồn tại hoặc đã được thuê' AS Message, 0 AS MaYeuCau, 0 AS MaThanhToan;
            RETURN;
        END

        -- Kiểm tra đã có yêu cầu pending chưa
        IF EXISTS (SELECT 1 FROM YEUCAU_THUEPHONG
                   WHERE PhongId = @PhongId AND MaTenant = @MaTenant
                   AND TrangThai IN ('PendingPayment', 'WaitingConfirm', 'PendingApprove'))
        BEGIN
            SELECT 0 AS Success, N'Bạn đã có yêu cầu thuê phòng này đang chờ xử lý' AS Message, 0 AS MaYeuCau, 0 AS MaThanhToan;
            RETURN;
        END

        -- Lấy cấu hình thanh toán
        IF @BankConfigId IS NULL
            SELECT TOP 1 @BankConfigId = ConfigId, @DepositMonths = DepositMonths, @TransferTemplate = TransferTemplate
            FROM PAYMENT_CONFIG WHERE IsActive = 1;
        ELSE
            SELECT @DepositMonths = DepositMonths, @TransferTemplate = TransferTemplate
            FROM PAYMENT_CONFIG WHERE ConfigId = @BankConfigId;

        SET @SoTienCoc = @GiaPhong * ISNULL(@DepositMonths, 1);

        -- ✅ FIX #8: Tạo yêu cầu với GiaPhongKhiGui
        INSERT INTO YEUCAU_THUEPHONG (PhongId, MaTenant, NgayBatDauMongMuon, SoNguoi, GhiChu, TrangThai, GiaPhongKhiGui, NgayGui)
        VALUES (@PhongId, @MaTenant, @NgayBatDauMongMuon, @SoNguoi, @GhiChu, 'PendingPayment', @GiaPhong, GETDATE());

        SET @MaYeuCau = SCOPE_IDENTITY();

        -- Tạo nội dung chuyển khoản
        SET @NoiDungCK = REPLACE(REPLACE(@TransferTemplate, '{MaYeuCau}', CAST(@MaYeuCau AS NVARCHAR(10))), '{MaPhong}', @MaPhong);

        -- Tạo phiếu thanh toán
        INSERT INTO BOOKING_PAYMENT (MaYeuCau, SoTien, NoiDungChuyenKhoan, TrangThai, BankConfigId)
        VALUES (@MaYeuCau, @SoTienCoc, @NoiDungCK, 'Pending', @BankConfigId);

        DECLARE @MaThanhToan INT = SCOPE_IDENTITY();

        -- Thiết lập hạn thanh toán (24 giờ)
        UPDATE YEUCAU_THUEPHONG
        SET NgayHetHan = DATEADD(HOUR, 24, GETDATE())
        WHERE MaYeuCau = @MaYeuCau;

        -- Tạo thông báo cho Admin
        INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
        SELECT UserId, N'Yêu cầu thuê phòng mới',
               N'Có yêu cầu thuê phòng ' + @MaPhong + N' mới cần thanh toán cọc',
               'ThuePhong', CAST(@MaYeuCau AS NVARCHAR(50))
        FROM USERS WHERE RoleId IN (SELECT RoleId FROM ROLES WHERE RoleName IN ('Admin', 'Manager'));

        COMMIT TRANSACTION;
        SELECT 1 AS Success, N'Tạo yêu cầu thành công' AS Message, @MaYeuCau AS MaYeuCau, @MaThanhToan AS MaThanhToan;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 AS Success, ERROR_MESSAGE() AS Message, 0 AS MaYeuCau, 0 AS MaThanhToan;
    END CATCH
END
GO

PRINT N'✅ Đã cập nhật sp_CreateBookingWithPayment';
GO

-- =====================================================
-- 5. MIGRATE DỮ LIỆU CŨ (Backfill GiaPhongKhiGui)
-- =====================================================

-- Cập nhật GiaPhongKhiGui cho các yêu cầu chưa có giá
UPDATE yc
SET yc.GiaPhongKhiGui = p.GiaThue
FROM YEUCAU_THUEPHONG yc
INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
WHERE yc.GiaPhongKhiGui IS NULL;

PRINT N'✅ Đã migrate dữ liệu cũ';
GO

-- =====================================================
-- SUMMARY
-- =====================================================

PRINT N'';
PRINT N'========================================';
PRINT N'✅ HOÀN THÀNH FIX CRITICAL ISSUES';
PRINT N'========================================';
PRINT N'1. ✓ Thêm sp_AutoExpireContracts';
PRINT N'2. ✓ Thêm GiaPhongKhiGui vào YEUCAU_THUEPHONG';
PRINT N'3. ✓ Thêm validation số người trong sp_ApproveBookingRequest';
PRINT N'4. ✓ Thêm kiểm tra hợp đồng chồng chéo';
PRINT N'5. ✓ Lưu giá phòng khi tạo yêu cầu';
PRINT N'6. ✓ Cập nhật sp_CreateBookingWithPayment';
PRINT N'';
PRINT N'📝 GHI CHÚ:';
PRINT N'   - Cần tạo Background Service để chạy sp_AutoExpireContracts';
PRINT N'   - Cần fix UI để không cho sửa tiền cọc khi duyệt';
PRINT N'   - Cần fix HoaDonService.CreateBatchAsync()';
PRINT N'========================================';
GO
