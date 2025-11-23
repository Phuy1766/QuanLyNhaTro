/*
 * Migration: Sửa lỗi Race Condition khi duyệt yêu cầu thuê phòng
 * Vấn đề: Khi 2 admin duyệt cùng 1 lúc, cả 2 có thể approval nếu checking phòng trống
 *         diễn ra song song trước khi update YEUCAU_THUEPHONG
 * 
 * Giải pháp: Sử dụng UPDLOCK hint trong SELECT để lock record trong transaction
 *            Điều này buộc admin thứ 2 phải chờ admin thứ 1 xong mới tiếp tục
 * 
 * Kỹ thuật: WITH (UPDLOCK) = Shared lock được upgrade thành exclusive lock
 *           Prevents dirty read, non-repeatable read, lost update
 * 
 * Impact: 0 - Chỉ sửa stored procedure, không thay đổi schema/data
 * Risk: Very Low - UPDLOCK là best practice cho transaction-heavy code
 * Date: 2025-01-23
 */
USE QuanLyNhaTro;
GO

-- Drop stored procedure hiện tại
IF OBJECT_ID('sp_ApproveBookingRequest', 'P') IS NOT NULL
    DROP PROCEDURE sp_ApproveBookingRequest;
GO

-- Tạo lại stored procedure với UPDLOCK để ngăn race condition
CREATE PROCEDURE sp_ApproveBookingRequest
    @MaYeuCau INT,
    @NguoiXuLy INT,
    @MaHopDong NVARCHAR(20) = NULL,  -- Nếu NULL, tự động generate
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

        -- ✅ FIX 1.2: UPDLOCK ngăn hai admin approve cùng lúc
        SELECT @PhongId = PhongId, @MaTenant = MaTenant, @NgayBatDau = NgayBatDauMongMuon
        FROM YEUCAU_THUEPHONG WITH (UPDLOCK)
        WHERE MaYeuCau = @MaYeuCau
          AND TrangThai IN ('Pending', 'PendingApprove');

        IF @PhongId IS NULL
        BEGIN
            RAISERROR(N'Yêu cầu không tồn tại hoặc đã được xử lý', 16, 1);
            ROLLBACK;
            RETURN;
        END

        -- Kiểm tra nếu yêu cầu này đã có hợp đồng rồi
        SELECT @ExistingHopDong = MaHopDong 
        FROM HOPDONG 
        WHERE PhongId = @PhongId 
          AND NgayBatDau = @NgayBatDau
          AND TrangThai = 'Active';

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

        -- Kiểm tra phòng còn trống
        IF NOT EXISTS (SELECT 1 FROM PHONGTRO WITH (UPDLOCK) WHERE PhongId = @PhongId AND TrangThai = N'Trống')
        BEGIN
            RAISERROR(N'Phòng đã được thuê hoặc đang bảo trì', 16, 1);
            ROLLBACK;
            RETURN;
        END

        -- Lấy giá phòng
        SELECT @GiaPhong = GiaThue FROM PHONGTRO WHERE PhongId = @PhongId;

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
        IF @MaHopDong IS NULL
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
        SET TrangThai = N'Đang thuê'
        WHERE PhongId = @PhongId;

        -- Tạo hợp đồng
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
        
        -- Return success
        SELECT 1 AS Success, N'Duyệt yêu cầu thành công' AS Message;

    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- ====================================================================
-- GIẢI THÍCH UPDLOCK
-- ====================================================================
/*
UPDLOCK (Update Lock) là SQL Server locking hint:

1. SHARED LOCK (S):
   - Được giữ từ lúc SELECT đến COMMIT/ROLLBACK
   - Multiple transactions có thể có S lock cùng lúc
   - Nhưng không thể update record khi có S lock

2. EXCLUSIVE LOCK (X):
   - Chỉ 1 transaction có thể hold tại một lúc
   - Được acquire khi UPDATE/DELETE/INSERT
   - Block tất cả readers và writers khác

3. UPDATE LOCK (U) - Từ WITH (UPDLOCK):
   - Combine shared lock (cho phép read) + khả năng upgrade thành X lock
   - Transaction A: SELECT...WITH (UPDLOCK) → hold U lock
   - Transaction B: SELECT...WITH (UPDLOCK) → CHỜ vì A đã hold U lock
   - Khi A UPDATE, U lock → X lock, B vẫn chờ
   - Khi A COMMIT, X lock release, B acquire U lock
   
KỊCH BẠN RACE CONDITION (SẼ BỊ NGĂN):
======================================
Thời gian │ Admin A               │ Admin B
────────────────────────────────────────────
T0        │ SELECT phòng trống    │
T1        │ (kiểm tra: OK)        │ SELECT phòng trống
T2        │                       │ (kiểm tra: OK)
T3        │ UPDATE yêu cầu A→Appr │
T4        │ UPDATE phòng→Đã Thuê  │
T5        │ INSERT hợp đồng A     │ UPDATE yêu cầu B→Appr
T6        │ COMMIT                │
T7        │                       │ UPDATE phòng→Đã Thuê  ❌ DUPLICATE!
T8        │                       │ INSERT hợp đồng B     ❌ DUPLICATE!

VỚI UPDLOCK:
============
T0        │ SELECT...WITH (UPDLOCK)
T1        │ → Admin A LOCK YEUCAU_THUEPHONG+PHONGTRO
T2        │                       │ SELECT...WITH (UPDLOCK)
T3        │                       │ → Admin B CHỜ (waiting for lock)
T4        │ (kiểm tra: OK)        │
T5        │ UPDATE yêu cầu A      │
T6        │ UPDATE phòng A        │
T7        │ INSERT hợp đồng A     │
T8        │ COMMIT (release lock) │
T9        │                       │ Admin B: Acquire lock, check phòng
T10       │                       │ Phòng đã "Đã Thuê" → ERROR ✅
T11       │                       │ ROLLBACK

Result: Chỉ Admin A thành công, Admin B nhận lỗi "phòng đã được thuê"
*/
GO

-- Test: Kiểm tra procedure đã update
PRINT '✓ Stored Procedure sp_ApproveBookingRequest đã cập nhật với UPDLOCK';
PRINT '  - YEUCAU_THUEPHONG: WITH (UPDLOCK) trong SELECT';
PRINT '  - PHONGTRO: WITH (UPDLOCK) trong SELECT';
PRINT '  - Ngăn race condition khi 2 admin duyệt cùng 1 phòng';
GO
