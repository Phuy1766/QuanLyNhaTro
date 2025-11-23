/*
 * MIGRATION: Thêm Database Constraints cho validation
 * FIX Issue 8.1: Thêm CHECK constraints để enforce business rules
 * Date: 2025-01-23
 */

USE QuanLyNhaTro;
GO

-- =====================================================
-- ADD CONSTRAINTS: HOPDONG
-- =====================================================

-- Kiểm tra NgayKetThuc > NgayBatDau
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_HopDong_NgayKetThuc_After_NgayBatDau')
BEGIN
    ALTER TABLE HOPDONG
    ADD CONSTRAINT CHK_HopDong_NgayKetThuc_After_NgayBatDau 
    CHECK (NgayKetThuc > NgayBatDau);
    
    PRINT N'✅ Đã thêm constraint: NgayKetThuc > NgayBatDau cho HOPDONG';
END
GO

-- Kiểm tra GiaThue > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_HopDong_GiaThue_Positive')
BEGIN
    ALTER TABLE HOPDONG
    ADD CONSTRAINT CHK_HopDong_GiaThue_Positive 
    CHECK (GiaThue > 0);
    
    PRINT N'✅ Đã thêm constraint: GiaThue > 0 cho HOPDONG';
END
GO

-- Kiểm tra TienCoc >= 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_HopDong_TienCoc_NonNegative')
BEGIN
    ALTER TABLE HOPDONG
    ADD CONSTRAINT CHK_HopDong_TienCoc_NonNegative 
    CHECK (TienCoc >= 0);
    
    PRINT N'✅ Đã thêm constraint: TienCoc >= 0 cho HOPDONG';
END
GO

-- =====================================================
-- ADD CONSTRAINTS: PHONGTRO
-- =====================================================

-- Kiểm tra GiaThue > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_PhongTro_GiaThue_Positive')
BEGIN
    ALTER TABLE PHONGTRO
    ADD CONSTRAINT CHK_PhongTro_GiaThue_Positive 
    CHECK (GiaThue > 0);
    
    PRINT N'✅ Đã thêm constraint: GiaThue > 0 cho PHONGTRO';
END
GO

-- Kiểm tra SoNguoiToiDa > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_PhongTro_SoNguoiToiDa_Positive')
BEGIN
    ALTER TABLE PHONGTRO
    ADD CONSTRAINT CHK_PhongTro_SoNguoiToiDa_Positive 
    CHECK (SoNguoiToiDa > 0);
    
    PRINT N'✅ Đã thêm constraint: SoNguoiToiDa > 0 cho PHONGTRO';
END
GO

-- =====================================================
-- ADD CONSTRAINTS: HOADON
-- =====================================================

-- Kiểm tra TongCong = TienPhong + TongTienDichVu
-- Note: Có thể là trigger nếu muốn enforce
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_HoaDon_TongCong_NonNegative')
BEGIN
    ALTER TABLE HOADON
    ADD CONSTRAINT CHK_HoaDon_TongCong_NonNegative 
    CHECK (TongCong >= 0);
    
    PRINT N'✅ Đã thêm constraint: TongCong >= 0 cho HOADON';
END
GO

-- Kiểm tra DaThanhToan <= TongCong
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_HoaDon_DaThanhToan_LessOrEqual_TongCong')
BEGIN
    ALTER TABLE HOADON
    ADD CONSTRAINT CHK_HoaDon_DaThanhToan_LessOrEqual_TongCong 
    CHECK (DaThanhToan <= TongCong);
    
    PRINT N'✅ Đã thêm constraint: DaThanhToan <= TongCong cho HOADON';
END
GO

-- =====================================================
-- ADD CONSTRAINTS: YEUCAU_THUEPHONG
-- =====================================================

-- Kiểm tra SoNguoi > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_YeuCau_SoNguoi_Positive')
BEGIN
    ALTER TABLE YEUCAU_THUEPHONG
    ADD CONSTRAINT CHK_YeuCau_SoNguoi_Positive 
    CHECK (SoNguoi > 0);
    
    PRINT N'✅ Đã thêm constraint: SoNguoi > 0 cho YEUCAU_THUEPHONG';
END
GO

-- Kiểm tra NgayBatDauMongMuon >= Hôm nay
-- Note: Không thể dùng CHECK constraint vì GETDATE() không cho phép
-- Phải validate ở application layer hoặc trigger

-- =====================================================
-- ADD CONSTRAINTS: BOOKING_PAYMENT
-- =====================================================

-- Kiểm tra SoTien > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_BookingPayment_SoTien_Positive')
BEGIN
    ALTER TABLE BOOKING_PAYMENT
    ADD CONSTRAINT CHK_BookingPayment_SoTien_Positive 
    CHECK (SoTien > 0);
    
    PRINT N'✅ Đã thêm constraint: SoTien > 0 cho BOOKING_PAYMENT';
END
GO

-- =====================================================
-- ADD CONSTRAINTS: DAMAGE_REPORT
-- =====================================================

-- Kiểm tra GiaTriHuHong > 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints 
              WHERE name = 'CHK_DamageReport_GiaTriHuHong_Positive')
BEGIN
    ALTER TABLE DAMAGE_REPORT
    ADD CONSTRAINT CHK_DamageReport_GiaTriHuHong_Positive 
    CHECK (GiaTriHuHong > 0);
    
    PRINT N'✅ Đã thêm constraint: GiaTriHuHong > 0 cho DAMAGE_REPORT';
END
GO

PRINT N'========================================';
PRINT N'✅ Hoàn thành thêm DATABASE CONSTRAINTS';
PRINT N'========================================';
GO
