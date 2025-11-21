-- =====================================================
-- BẢNG YÊU CẦU THUÊ PHÒNG (BOOKING REQUEST)
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YEUCAU_THUEPHONG')
BEGIN
    CREATE TABLE YEUCAU_THUEPHONG (
        MaYeuCau INT IDENTITY(1,1) PRIMARY KEY,
        PhongId INT NOT NULL,                     -- FK → PHONGTRO (PhongId)
        MaTenant INT NOT NULL,                    -- FK → USERS (người gửi yêu cầu)
        NgayGui DATETIME DEFAULT GETDATE(),
        NgayBatDauMongMuon DATE NOT NULL,         -- Ngày muốn bắt đầu thuê
        SoNguoi INT DEFAULT 1,
        GhiChu NVARCHAR(500),
        TrangThai NVARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Rejected
        NgayXuLy DATETIME NULL,
        NguoiXuLy INT NULL,                       -- FK → USERS (Admin/Manager duyệt)
        LyDoTuChoi NVARCHAR(500) NULL,

        CONSTRAINT FK_YeuCau_Phong FOREIGN KEY (PhongId) REFERENCES PHONGTRO(PhongId),
        CONSTRAINT FK_YeuCau_Tenant FOREIGN KEY (MaTenant) REFERENCES USERS(UserId),
        CONSTRAINT FK_YeuCau_NguoiXuLy FOREIGN KEY (NguoiXuLy) REFERENCES USERS(UserId),
        CONSTRAINT CHK_TrangThai CHECK (TrangThai IN ('Pending', 'Approved', 'Rejected'))
    );

    PRINT N'Đã tạo bảng YEUCAU_THUEPHONG';
END
GO

-- Index cho tìm kiếm
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_YeuCau_TrangThai')
    CREATE NONCLUSTERED INDEX IX_YeuCau_TrangThai ON YEUCAU_THUEPHONG(TrangThai);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_YeuCau_MaTenant')
    CREATE NONCLUSTERED INDEX IX_YeuCau_MaTenant ON YEUCAU_THUEPHONG(MaTenant);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_YeuCau_PhongId')
    CREATE NONCLUSTERED INDEX IX_YeuCau_PhongId ON YEUCAU_THUEPHONG(PhongId);
GO

-- =====================================================
-- BẢNG THÔNG BÁO (NOTIFICATION)
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'THONGBAO')
BEGIN
    CREATE TABLE THONGBAO (
        MaThongBao INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,                      -- Người nhận thông báo
        TieuDe NVARCHAR(200) NOT NULL,
        NoiDung NVARCHAR(1000) NOT NULL,
        LoaiThongBao NVARCHAR(50) NOT NULL,       -- HoaDon, HopDong, BaoTri, ThuePhong, HeThong
        MaLienKet NVARCHAR(50) NULL,              -- ID liên quan (MaHoaDon, MaHopDong, MaYeuCau...)
        DaDoc BIT DEFAULT 0,
        NgayTao DATETIME DEFAULT GETDATE(),
        NgayDoc DATETIME NULL,

        CONSTRAINT FK_ThongBao_User FOREIGN KEY (UserId) REFERENCES USERS(UserId)
    );

    PRINT N'Đã tạo bảng THONGBAO';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ThongBao_User')
    CREATE NONCLUSTERED INDEX IX_ThongBao_User ON THONGBAO(UserId, DaDoc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ThongBao_NgayTao')
    CREATE NONCLUSTERED INDEX IX_ThongBao_NgayTao ON THONGBAO(NgayTao DESC);
GO

-- =====================================================
-- BẢNG TIN NHẮN HỖ TRỢ (CHAT/MESSAGE)
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TINNHAN')
BEGIN
    CREATE TABLE TINNHAN (
        MaTinNhan INT IDENTITY(1,1) PRIMARY KEY,
        NguoiGui INT NOT NULL,                    -- FK → USERS
        NguoiNhan INT NOT NULL,                   -- FK → USERS
        NoiDung NVARCHAR(2000) NOT NULL,
        ThoiGian DATETIME DEFAULT GETDATE(),
        DaDoc BIT DEFAULT 0,

        CONSTRAINT FK_TinNhan_NguoiGui FOREIGN KEY (NguoiGui) REFERENCES USERS(UserId),
        CONSTRAINT FK_TinNhan_NguoiNhan FOREIGN KEY (NguoiNhan) REFERENCES USERS(UserId)
    );

    PRINT N'Đã tạo bảng TINNHAN';
END
GO

-- =====================================================
-- STORED PROCEDURES CHO BOOKING
-- Chạy sau khi tất cả bảng đã được tạo
-- =====================================================

-- Drop existing procedures first
IF OBJECT_ID('sp_ApproveBookingRequest', 'P') IS NOT NULL
    DROP PROCEDURE sp_ApproveBookingRequest;
GO

IF OBJECT_ID('sp_RejectBookingRequest', 'P') IS NOT NULL
    DROP PROCEDURE sp_RejectBookingRequest;
GO

-- SP: Duyệt yêu cầu thuê phòng
CREATE PROCEDURE sp_ApproveBookingRequest
    @MaYeuCau INT,
    @NguoiXuLy INT,
    @MaHopDong NVARCHAR(20),
    @NgayKetThuc DATE,
    @TienCoc DECIMAL(18,2),
    @GhiChu NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @PhongId INT, @MaTenant INT, @NgayBatDau DATE, @GiaPhong DECIMAL(18,0);

        -- Lấy thông tin yêu cầu
        SELECT @PhongId = PhongId, @MaTenant = MaTenant, @NgayBatDau = NgayBatDauMongMuon
        FROM YEUCAU_THUEPHONG WHERE MaYeuCau = @MaYeuCau AND TrangThai = 'Pending';

        IF @PhongId IS NULL
        BEGIN
            RAISERROR(N'Yêu cầu không tồn tại hoặc đã được xử lý', 16, 1);
            RETURN;
        END

        -- Kiểm tra phòng còn trống
        IF NOT EXISTS (SELECT 1 FROM PHONGTRO WHERE PhongId = @PhongId AND TrangThai = N'Trống')
        BEGIN
            RAISERROR(N'Phòng đã được thuê hoặc đang bảo trì', 16, 1);
            RETURN;
        END

        -- Lấy giá phòng
        SELECT @GiaPhong = GiaThue FROM PHONGTRO WHERE PhongId = @PhongId;

        -- Lấy KhachId từ UserId
        DECLARE @KhachId INT;
        SELECT @KhachId = KhachId FROM KHACHTHUE WHERE UserId = @MaTenant;

        IF @KhachId IS NULL
        BEGIN
            RAISERROR(N'Không tìm thấy thông tin khách thuê', 16, 1);
            RETURN;
        END

        -- Tạo hợp đồng mới
        INSERT INTO HOPDONG (MaHopDong, PhongId, KhachId, NgayBatDau, NgayKetThuc, GiaThue, TienCoc, TrangThai, GhiChu)
        VALUES (@MaHopDong, @PhongId, @KhachId, @NgayBatDau, @NgayKetThuc, @GiaPhong, @TienCoc, N'Active', @GhiChu);

        -- Cập nhật trạng thái phòng
        UPDATE PHONGTRO SET TrangThai = N'Đang thuê' WHERE PhongId = @PhongId;

        -- Cập nhật yêu cầu
        UPDATE YEUCAU_THUEPHONG
        SET TrangThai = 'Approved', NgayXuLy = GETDATE(), NguoiXuLy = @NguoiXuLy
        WHERE MaYeuCau = @MaYeuCau;

        -- Lấy MaPhong để hiển thị thông báo
        DECLARE @MaPhong NVARCHAR(20);
        SELECT @MaPhong = MaPhong FROM PHONGTRO WHERE PhongId = @PhongId;

        -- Tạo thông báo cho Tenant
        INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
        VALUES (@MaTenant, N'Yêu cầu thuê phòng được duyệt',
                N'Yêu cầu thuê phòng ' + @MaPhong + N' của bạn đã được duyệt. Hợp đồng: ' + @MaHopDong,
                'ThuePhong', CAST(@MaYeuCau AS NVARCHAR(50)));

        COMMIT TRANSACTION;
        SELECT 1 AS Success, N'Duyệt yêu cầu thành công' AS Message;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 AS Success, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

-- SP: Từ chối yêu cầu thuê phòng
CREATE PROCEDURE sp_RejectBookingRequest
    @MaYeuCau INT,
    @NguoiXuLy INT,
    @LyDoTuChoi NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaTenant INT, @PhongId INT, @MaPhong NVARCHAR(20);

    SELECT @MaTenant = MaTenant, @PhongId = PhongId
    FROM YEUCAU_THUEPHONG WHERE MaYeuCau = @MaYeuCau AND TrangThai = 'Pending';

    IF @MaTenant IS NULL
    BEGIN
        SELECT 0 AS Success, N'Yêu cầu không tồn tại hoặc đã được xử lý' AS Message;
        RETURN;
    END

    -- Lấy MaPhong để hiển thị thông báo
    SELECT @MaPhong = MaPhong FROM PHONGTRO WHERE PhongId = @PhongId;

    -- Cập nhật yêu cầu
    UPDATE YEUCAU_THUEPHONG
    SET TrangThai = 'Rejected', NgayXuLy = GETDATE(), NguoiXuLy = @NguoiXuLy, LyDoTuChoi = @LyDoTuChoi
    WHERE MaYeuCau = @MaYeuCau;

    -- Tạo thông báo cho Tenant
    INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
    VALUES (@MaTenant, N'Yêu cầu thuê phòng bị từ chối',
            N'Yêu cầu thuê phòng ' + @MaPhong + N' đã bị từ chối. Lý do: ' + @LyDoTuChoi,
            'ThuePhong', CAST(@MaYeuCau AS NVARCHAR(50)));

    SELECT 1 AS Success, N'Đã từ chối yêu cầu' AS Message;
END
GO

PRINT N'Hoàn thành tạo bảng và stored procedures cho Booking & Notification';
GO
