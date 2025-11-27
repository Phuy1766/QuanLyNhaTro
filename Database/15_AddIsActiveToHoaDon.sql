USE QuanLyNhaTro;
GO

-- Add IsActive column to HOADON if not exists (soft delete support)
IF COL_LENGTH('dbo.HOADON', 'IsActive') IS NULL
BEGIN
    ALTER TABLE HOADON ADD IsActive BIT DEFAULT 1 NOT NULL;
    PRINT N'✅ Đã thêm cột IsActive cho bảng HOADON';

    -- Ensure all existing records are active
    UPDATE HOADON SET IsActive = 1 WHERE IsActive IS NULL;
END
ELSE
BEGIN
    PRINT N'ℹ️ Cột IsActive đã tồn tại trong bảng HOADON';
END
GO
