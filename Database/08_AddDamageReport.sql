/*
 * CREATE TABLE: DAMAGE_REPORT
 * Purpose: Ghi nhận hư hỏng tài sản phòng trọ
 * FIX for Issue 4.1: Cần có cơ chế chứng cứ + admin phê duyệt
 * Date: 2025-01-23
 */

USE QuanLyNhaTro;
GO

-- =====================================================
-- BẢNG GHI NHẬN HƯ HỎNG TÀI SẢN (DAMAGE REPORT)
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DAMAGE_REPORT')
BEGIN
    CREATE TABLE DAMAGE_REPORT (
        DamageId INT IDENTITY(1,1) PRIMARY KEY,
        HopDongId INT NOT NULL,                    -- FK → HOPDONG
        PhongId INT NOT NULL,                      -- FK → PHONGTRO
        TaiSanId INT NOT NULL,                     -- FK → TAISAN
        MoTa NVARCHAR(500) NOT NULL,               -- Mô tả hư hỏng
        MucDoHuHong NVARCHAR(50) DEFAULT 'Trung bình',  -- Nhẹ, Trung bình, Nặng
        GiaTriHuHong DECIMAL(18,0) NOT NULL,       -- Giá trị hư hỏng (VND)
        HinhAnhChungCu NVARCHAR(500) NULL,         -- Đường dẫn ảnh chứng cứ
        NgayGhiNhan DATETIME DEFAULT GETDATE(),
        NguoiGhiNhan INT,                          -- FK → USERS (Admin/Manager ghi nhận)
        TrangThai NVARCHAR(30) DEFAULT 'PendingApproval', -- PendingApproval, Approved, Rejected
        NgayPheDuyet DATETIME NULL,
        NguoiPheDuyet INT,                         -- FK → USERS (Admin phê duyệt)
        LyDoTuChoi NVARCHAR(500) NULL,             -- Nếu từ chối
        GhiChu NVARCHAR(500) NULL,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,

        CONSTRAINT FK_Damage_HopDong FOREIGN KEY (HopDongId) REFERENCES HOPDONG(HopDongId),
        CONSTRAINT FK_Damage_Phong FOREIGN KEY (PhongId) REFERENCES PHONGTRO(PhongId),
        CONSTRAINT FK_Damage_TaiSan FOREIGN KEY (TaiSanId) REFERENCES TAISAN(TaiSanId),
        CONSTRAINT FK_Damage_NguoiGhiNhan FOREIGN KEY (NguoiGhiNhan) REFERENCES USERS(UserId),
        CONSTRAINT FK_Damage_NguoiPheDuyet FOREIGN KEY (NguoiPheDuyet) REFERENCES USERS(UserId),
        CONSTRAINT CHK_MucDoHuHong CHECK (MucDoHuHong IN (N'Nhẹ', N'Trung bình', N'Nặng')),
        CONSTRAINT CHK_DamageStatus CHECK (TrangThai IN ('PendingApproval', 'Approved', 'Rejected'))
    );

    PRINT N'✅ Đã tạo bảng DAMAGE_REPORT';
END
GO

-- Tạo Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Damage_HopDongId')
    CREATE NONCLUSTERED INDEX IX_Damage_HopDongId ON DAMAGE_REPORT(HopDongId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Damage_TrangThai')
    CREATE NONCLUSTERED INDEX IX_Damage_TrangThai ON DAMAGE_REPORT(TrangThai);
GO

-- =====================================================
-- STORED PROCEDURE: GHI NHẬN HƯ HỎNG
-- =====================================================

IF OBJECT_ID('sp_CreateDamageReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreateDamageReport;
GO

CREATE PROCEDURE sp_CreateDamageReport
    @HopDongId INT,
    @PhongId INT,
    @TaiSanId INT,
    @MoTa NVARCHAR(500),
    @MucDoHuHong NVARCHAR(50),
    @GiaTriHuHong DECIMAL(18,0),
    @HinhAnhChungCu NVARCHAR(500) = NULL,
    @NguoiGhiNhan INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validation
    IF @GiaTriHuHong < 0
    BEGIN
        SELECT 0 AS Success, N'Giá trị hư hỏng không được âm' AS Message, 0 AS DamageId;
        RETURN;
    END

    -- Tạo ghi nhận
    INSERT INTO DAMAGE_REPORT (HopDongId, PhongId, TaiSanId, MoTa, MucDoHuHong, GiaTriHuHong, HinhAnhChungCu, NguoiGhiNhan)
    VALUES (@HopDongId, @PhongId, @TaiSanId, @MoTa, @MucDoHuHong, @GiaTriHuHong, @HinhAnhChungCu, @NguoiGhiNhan);

    DECLARE @DamageId INT = SCOPE_IDENTITY();

    -- Thông báo cho Admin
    DECLARE @TenTaiSan NVARCHAR(50), @MaPhong NVARCHAR(20);
    SELECT @TenTaiSan = TenTaiSan FROM TAISAN WHERE TaiSanId = @TaiSanId;
    SELECT @MaPhong = MaPhong FROM PHONGTRO WHERE PhongId = @PhongId;

    INSERT INTO THONGBAO (UserId, TieuDe, NoiDung, LoaiThongBao, MaLienKet)
    SELECT UserId, N'Ghi nhận hư hỏng tài sản',
           N'Phòng ' + @MaPhong + N' có tài sản ' + @TenTaiSan + N' bị hư hỏng. Cần phê duyệt.',
           'BaoTri', CAST(@DamageId AS NVARCHAR(50))
    FROM USERS WHERE RoleId IN (SELECT RoleId FROM ROLES WHERE RoleName = 'Admin');

    SELECT 1 AS Success, N'Đã ghi nhận hư hỏng. Chờ Admin phê duyệt.' AS Message, @DamageId AS DamageId;
END
GO

-- =====================================================
-- STORED PROCEDURE: PHÊ DUYỆT/TỪ CHỐI HƯ HỎNG
-- =====================================================

IF OBJECT_ID('sp_ApproveDamageReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_ApproveDamageReport;
GO

CREATE PROCEDURE sp_ApproveDamageReport
    @DamageId INT,
    @NguoiPheDuyet INT,
    @IsApproved BIT,
    @LyDoTuChoi NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @HopDongId INT, @GiaTriHuHong DECIMAL(18,0), @MaPhong NVARCHAR(20);

        SELECT @HopDongId = HopDongId, @GiaTriHuHong = GiaTriHuHong, @MaPhong = p.MaPhong
        FROM DAMAGE_REPORT dr
        INNER JOIN PHONGTRO p ON dr.PhongId = p.PhongId
        WHERE dr.DamageId = @DamageId AND dr.TrangThai = 'PendingApproval';

        IF @HopDongId IS NULL
        BEGIN
            SELECT 0 AS Success, N'Ghi nhận hư hỏng không tồn tại hoặc đã được xử lý' AS Message;
            RETURN;
        END

        IF @IsApproved = 1
        BEGIN
            -- Phê duyệt
            UPDATE DAMAGE_REPORT
            SET TrangThai = 'Approved', NgayPheDuyet = GETDATE(), NguoiPheDuyet = @NguoiPheDuyet
            WHERE DamageId = @DamageId;

            -- Cộng khấu trừ vào hợp đồng (khi thanh lý sẽ tính)
            -- Note: Logic này sẽ được dùng trong TerminateAsync của HopDongService
        END
        ELSE
        BEGIN
            -- Từ chối
            UPDATE DAMAGE_REPORT
            SET TrangThai = 'Rejected', NgayPheDuyet = GETDATE(), NguoiPheDuyet = @NguoiPheDuyet, LyDoTuChoi = @LyDoTuChoi
            WHERE DamageId = @DamageId;
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Success, CASE WHEN @IsApproved = 1 THEN N'Đã phê duyệt hư hỏng' ELSE N'Đã từ chối hư hỏng' END AS Message;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 AS Success, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

-- =====================================================
-- FUNCTION: TÍNH TỔNG GIÁ TRỊ HƯ HỎNG ĐƯỢC PHÊ DUYỆT CỦA HỢP ĐỒNG
-- =====================================================

IF OBJECT_ID('fn_GetTotalApprovedDamageByContract', 'FN') IS NOT NULL
    DROP FUNCTION fn_GetTotalApprovedDamageByContract;
GO

CREATE FUNCTION fn_GetTotalApprovedDamageByContract(@HopDongId INT)
RETURNS DECIMAL(18,0)
AS
BEGIN
    DECLARE @Total DECIMAL(18,0);
    
    SELECT @Total = ISNULL(SUM(GiaTriHuHong), 0)
    FROM DAMAGE_REPORT
    WHERE HopDongId = @HopDongId AND TrangThai = 'Approved';
    
    RETURN @Total;
END
GO

PRINT N'✅ Hoàn thành tạo DAMAGE_REPORT system';
GO
