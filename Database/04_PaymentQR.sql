-- =====================================================
-- BẢNG CẤU HÌNH THANH TOÁN (PAYMENT CONFIG)
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAYMENT_CONFIG')
BEGIN
    CREATE TABLE PAYMENT_CONFIG (
        ConfigId INT IDENTITY(1,1) PRIMARY KEY,
        BankName NVARCHAR(100) NOT NULL,           -- Tên ngân hàng/ví điện tử
        AccountName NVARCHAR(100) NOT NULL,        -- Tên chủ tài khoản
        AccountNumber NVARCHAR(50) NOT NULL,       -- Số tài khoản/số ví
        BankCode NVARCHAR(20) NULL,                -- Mã ngân hàng (VCB, TCB, MB...)
        TransferTemplate NVARCHAR(200) DEFAULT 'NTPRO_{MaYeuCau}_{MaPhong}', -- Template nội dung CK
        DepositMonths INT DEFAULT 1,               -- Số tháng tiền cọc yêu cầu
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    );

    PRINT N'Đã tạo bảng PAYMENT_CONFIG';
END
GO

-- =====================================================
-- BẢNG PHIẾU THANH TOÁN CỌC (BOOKING PAYMENT)
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BOOKING_PAYMENT')
BEGIN
    CREATE TABLE BOOKING_PAYMENT (
        MaThanhToan INT IDENTITY(1,1) PRIMARY KEY,
        MaYeuCau INT NOT NULL,                     -- FK → YEUCAU_THUEPHONG
        SoTien DECIMAL(18,0) NOT NULL,             -- Số tiền cọc
        NoiDungChuyenKhoan NVARCHAR(100) NOT NULL, -- Nội dung CK bắt buộc
        TrangThai NVARCHAR(30) DEFAULT 'Pending',  -- Pending, WaitingConfirm, Paid, Canceled, Refunded
        KieuThanhToan NVARCHAR(30) DEFAULT 'QRBank', -- QRBank, MoMo, ZaloPay, Cash
        BankConfigId INT NULL,                     -- FK → PAYMENT_CONFIG
        NgayTao DATETIME DEFAULT GETDATE(),
        NgayThanhToan DATETIME NULL,
        NgayXacNhan DATETIME NULL,
        NguoiXacNhan INT NULL,                     -- FK → USERS (Admin/Manager xác nhận)
        GhiChu NVARCHAR(500) NULL,

        CONSTRAINT FK_BookingPayment_YeuCau FOREIGN KEY (MaYeuCau) REFERENCES YEUCAU_THUEPHONG(MaYeuCau),
        CONSTRAINT FK_BookingPayment_Config FOREIGN KEY (BankConfigId) REFERENCES PAYMENT_CONFIG(ConfigId),
        CONSTRAINT FK_BookingPayment_NguoiXacNhan FOREIGN KEY (NguoiXacNhan) REFERENCES USERS(UserId),
        CONSTRAINT CHK_PaymentTrangThai CHECK (TrangThai IN ('Pending', 'WaitingConfirm', 'Paid', 'Canceled', 'Refunded'))
    );

    PRINT N'Đã tạo bảng BOOKING_PAYMENT';
END
GO

-- Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BookingPayment_MaYeuCau')
    CREATE NONCLUSTERED INDEX IX_BookingPayment_MaYeuCau ON BOOKING_PAYMENT(MaYeuCau);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BookingPayment_TrangThai')
    CREATE NONCLUSTERED INDEX IX_BookingPayment_TrangThai ON BOOKING_PAYMENT(TrangThai);
GO

-- =====================================================
-- CẬP NHẬT BẢNG YEUCAU_THUEPHONG (thêm trạng thái mới)
-- =====================================================

-- Cập nhật constraint cho TrangThai mới
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CHK_TrangThai')
BEGIN
    ALTER TABLE YEUCAU_THUEPHONG DROP CONSTRAINT CHK_TrangThai;
END
GO

ALTER TABLE YEUCAU_THUEPHONG
ADD CONSTRAINT CHK_TrangThai CHECK (TrangThai IN ('PendingPayment', 'WaitingConfirm', 'PendingApprove', 'Approved', 'Rejected', 'Canceled'));
GO

-- =====================================================
-- DỮ LIỆU MẪU - CẤU HÌNH THANH TOÁN
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM PAYMENT_CONFIG)
BEGIN
    INSERT INTO PAYMENT_CONFIG (BankName, AccountName, AccountNumber, BankCode, TransferTemplate, DepositMonths)
    VALUES
        (N'Vietcombank', N'PHẠM QUANG HUY', '1030928755', 'VCB', 'NTPRO_{MaYeuCau}_{MaPhong}', 1),
        (N'MB Bank', N'PHẠM QUANG HUY', '0333591375', 'MB', 'NTPRO_{MaYeuCau}_{MaPhong}', 1),
        (N'MoMo', N'PHẠM QUANG HUY', '0333591375', 'MOMO', 'NTPRO_{MaYeuCau}_{MaPhong}', 1);

    PRINT N'Đã thêm dữ liệu mẫu PAYMENT_CONFIG';
END
GO

-- =====================================================
-- STORED PROCEDURE: TẠO YÊU CẦU THUÊ + PHIẾU THANH TOÁN
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

        -- Tạo yêu cầu thuê phòng
        INSERT INTO YEUCAU_THUEPHONG (PhongId, MaTenant, NgayBatDauMongMuon, SoNguoi, GhiChu, TrangThai)
        VALUES (@PhongId, @MaTenant, @NgayBatDauMongMuon, @SoNguoi, @GhiChu, 'PendingPayment');

        SET @MaYeuCau = SCOPE_IDENTITY();

        -- Tạo nội dung chuyển khoản
        SET @NoiDungCK = REPLACE(REPLACE(@TransferTemplate, '{MaYeuCau}', CAST(@MaYeuCau AS NVARCHAR(10))), '{MaPhong}', @MaPhong);

        -- Tạo phiếu thanh toán
        INSERT INTO BOOKING_PAYMENT (MaYeuCau, SoTien, NoiDungChuyenKhoan, TrangThai, BankConfigId)
        VALUES (@MaYeuCau, @SoTienCoc, @NoiDungCK, 'Pending', @BankConfigId);

        DECLARE @MaThanhToan INT = SCOPE_IDENTITY();

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

-- =====================================================
-- STORED PROCEDURE: XÁC NHẬN ĐÃ THANH TOÁN (TENANT)
-- =====================================================

IF OBJECT_ID('sp_ConfirmPaymentByTenant', 'P') IS NOT NULL
    DROP PROCEDURE sp_ConfirmPaymentByTenant;
GO

CREATE PROCEDURE sp_ConfirmPaymentByTenant
    @MaThanhToan INT,
    @MaTenant INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra quyền
    IF NOT EXISTS (SELECT 1 FROM BOOKING_PAYMENT bp
                   INNER JOIN YEUCAU_THUEPHONG yc ON bp.MaYeuCau = yc.MaYeuCau
                   WHERE bp.MaThanhToan = @MaThanhToan AND yc.MaTenant = @MaTenant AND bp.TrangThai = 'Pending')
    BEGIN
        SELECT 0 AS Success, N'Không tìm thấy phiếu thanh toán hoặc không có quyền' AS Message;
        RETURN;
    END

    -- Cập nhật trạng thái
    UPDATE BOOKING_PAYMENT
    SET TrangThai = 'WaitingConfirm', NgayThanhToan = GETDATE()
    WHERE MaThanhToan = @MaThanhToan;

    UPDATE YEUCAU_THUEPHONG
    SET TrangThai = 'WaitingConfirm'
    WHERE MaYeuCau = (SELECT MaYeuCau FROM BOOKING_PAYMENT WHERE MaThanhToan = @MaThanhToan);

    -- Thông báo cho Admin
    DECLARE @MaYeuCau INT, @MaPhong NVARCHAR(20);
    SELECT @MaYeuCau = bp.MaYeuCau, @MaPhong = p.MaPhong
    FROM BOOKING_PAYMENT bp
    INNER JOIN YEUCAU_THUEPHONG yc ON bp.MaYeuCau = yc.MaYeuCau
    INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
    WHERE bp.MaThanhToan = @MaThanhToan;

    INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
    SELECT UserId, N'Cần xác nhận thanh toán cọc',
           N'Khách đã báo thanh toán cọc phòng ' + @MaPhong + N'. Vui lòng kiểm tra và xác nhận.',
           'ThuePhong', CAST(@MaYeuCau AS NVARCHAR(50))
    FROM USERS WHERE RoleId IN (SELECT RoleId FROM ROLES WHERE RoleName IN ('Admin', 'Manager'));

    SELECT 1 AS Success, N'Đã gửi xác nhận thanh toán. Vui lòng chờ quản lý xác nhận.' AS Message;
END
GO

-- =====================================================
-- STORED PROCEDURE: XÁC NHẬN THANH TOÁN (ADMIN)
-- =====================================================

IF OBJECT_ID('sp_AdminConfirmPayment', 'P') IS NOT NULL
    DROP PROCEDURE sp_AdminConfirmPayment;
GO

CREATE PROCEDURE sp_AdminConfirmPayment
    @MaThanhToan INT,
    @NguoiXacNhan INT,
    @IsConfirmed BIT,  -- 1 = Xác nhận, 0 = Hủy
    @GhiChu NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaYeuCau INT, @MaTenant INT, @MaPhong NVARCHAR(20);

    SELECT @MaYeuCau = bp.MaYeuCau, @MaTenant = yc.MaTenant, @MaPhong = p.MaPhong
    FROM BOOKING_PAYMENT bp
    INNER JOIN YEUCAU_THUEPHONG yc ON bp.MaYeuCau = yc.MaYeuCau
    INNER JOIN PHONGTRO p ON yc.PhongId = p.PhongId
    WHERE bp.MaThanhToan = @MaThanhToan AND bp.TrangThai = 'WaitingConfirm';

    IF @MaYeuCau IS NULL
    BEGIN
        SELECT 0 AS Success, N'Không tìm thấy phiếu thanh toán cần xác nhận' AS Message;
        RETURN;
    END

    IF @IsConfirmed = 1
    BEGIN
        -- Xác nhận đã nhận tiền
        UPDATE BOOKING_PAYMENT
        SET TrangThai = 'Paid', NgayXacNhan = GETDATE(), NguoiXacNhan = @NguoiXacNhan, GhiChu = @GhiChu
        WHERE MaThanhToan = @MaThanhToan;

        UPDATE YEUCAU_THUEPHONG
        SET TrangThai = 'PendingApprove'
        WHERE MaYeuCau = @MaYeuCau;

        -- Thông báo cho Tenant
        INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
        VALUES (@MaTenant, N'Thanh toán cọc thành công',
                N'Thanh toán cọc phòng ' + @MaPhong + N' đã được xác nhận. Yêu cầu của bạn đang chờ duyệt hợp đồng.',
                'ThuePhong', CAST(@MaYeuCau AS NVARCHAR(50)));

        SELECT 1 AS Success, N'Đã xác nhận thanh toán thành công' AS Message;
    END
    ELSE
    BEGIN
        -- Hủy/từ chối thanh toán
        UPDATE BOOKING_PAYMENT
        SET TrangThai = 'Canceled', NgayXacNhan = GETDATE(), NguoiXacNhan = @NguoiXacNhan, GhiChu = @GhiChu
        WHERE MaThanhToan = @MaThanhToan;

        UPDATE YEUCAU_THUEPHONG
        SET TrangThai = 'Canceled'
        WHERE MaYeuCau = @MaYeuCau;

        -- Thông báo cho Tenant
        INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
        VALUES (@MaTenant, N'Thanh toán cọc bị từ chối',
                N'Thanh toán cọc phòng ' + @MaPhong + N' không được xác nhận. Lý do: ' + ISNULL(@GhiChu, N'Không xác định'),
                'ThuePhong', CAST(@MaYeuCau AS NVARCHAR(50)));

        SELECT 1 AS Success, N'Đã hủy giao dịch' AS Message;
    END
END
GO

PRINT N'Hoàn thành tạo bảng và stored procedures cho Payment QR';
GO
