/*
 * Migration: Sửa lỗi workflow duyệt yêu cầu thuê phòng
 * Vấn đề: sp_ApproveBookingRequest chỉ duyệt yêu cầu có TrangThai = 'Pending'
 *         nhưng sau khi tenant thanh toán và admin xác nhận, trạng thái đã là 'PendingApprove'
 * Giải pháp: Cho phép duyệt cả 'Pending' và 'PendingApprove'
 * Date: 2025-01-23
 */

USE QuanLyNhaTro;
GO

-- Drop stored procedure hiện tại
IF OBJECT_ID('sp_ApproveBookingRequest', 'P') IS NOT NULL
    DROP PROCEDURE sp_ApproveBookingRequest;
GO

-- Tạo lại stored procedure với logic đã sửa
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

        -- ✅ SỬA: Cho phép duyệt cả 'Pending' VÀ 'PendingApprove'
        SELECT @PhongId = PhongId, @MaTenant = MaTenant, @NgayBatDau = NgayBatDauMongMuon
        FROM YEUCAU_THUEPHONG
        WHERE MaYeuCau = @MaYeuCau
          AND TrangThai IN ('Pending', 'PendingApprove');  -- <-- ĐÃ SỬA: từ = 'Pending' thành IN (...)

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
        INSERT INTO HOPDONG (MaHopDong, PhongId, KhachId, NgayBatDau, NgayKetThuc, GiaThue, TienCoc, TrangThai, GhiChu, CreatedBy)
        VALUES (@MaHopDong, @PhongId, @KhachId, @NgayBatDau, @NgayKetThuc, @GiaPhong, @TienCoc, N'Active', @GhiChu, @NguoiXuLy);

        -- Cập nhật trạng thái phòng
        UPDATE PHONGTRO SET TrangThai = N'Đang thuê', UpdatedAt = GETDATE() WHERE PhongId = @PhongId;

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

PRINT N'✅ Đã sửa stored procedure sp_ApproveBookingRequest - Cho phép duyệt yêu cầu có trạng thái PendingApprove';
GO
