-- =====================================================
-- QUẢN LÝ NHÀ TRỌ - DATABASE SCRIPT
-- Version: 1.0 - 2025
-- Database: SQL Server
-- =====================================================

-- Tạo Database
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'QuanLyNhaTro')
BEGIN
    ALTER DATABASE QuanLyNhaTro SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QuanLyNhaTro;
END
GO

CREATE DATABASE QuanLyNhaTro;
GO

USE QuanLyNhaTro;
GO

-- =====================================================
-- 1. BẢNG ROLES - Vai trò người dùng
-- =====================================================
CREATE TABLE ROLES (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- =====================================================
-- 2. BẢNG USERS - Tài khoản người dùng
-- =====================================================
CREATE TABLE USERS (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Avatar NVARCHAR(255),
    RoleId INT NOT NULL,
    IsActive BIT DEFAULT 1,
    Theme NVARCHAR(20) DEFAULT 'Light', -- Light/Dark
    LastLogin DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES ROLES(RoleId)
);
GO

-- =====================================================
-- 3. BẢNG BUILDING - Tòa nhà
-- =====================================================
CREATE TABLE BUILDING (
    BuildingId INT IDENTITY(1,1) PRIMARY KEY,
    BuildingCode NVARCHAR(20) NOT NULL UNIQUE,
    BuildingName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255),
    TotalFloors INT DEFAULT 1,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME
);
GO

-- =====================================================
-- 4. BẢNG LOAIPHONG - Loại phòng
-- =====================================================
CREATE TABLE LOAIPHONG (
    LoaiPhongId INT IDENTITY(1,1) PRIMARY KEY,
    TenLoai NVARCHAR(50) NOT NULL,
    MoTa NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- =====================================================
-- 5. BẢNG PHONGTRO - Phòng trọ
-- =====================================================
CREATE TABLE PHONGTRO (
    PhongId INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong NVARCHAR(20) NOT NULL,
    BuildingId INT NOT NULL,
    LoaiPhongId INT,
    Tang INT DEFAULT 1,
    DienTich DECIMAL(10,2), -- m2
    GiaThue DECIMAL(18,0) NOT NULL, -- VND
    SoNguoiToiDa INT DEFAULT 2,
    TrangThai NVARCHAR(20) DEFAULT N'Trống', -- Trống, Đang thuê, Đang sửa
    MoTa NVARCHAR(500),
    HinhAnh NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT FK_PhongTro_Building FOREIGN KEY (BuildingId) REFERENCES BUILDING(BuildingId),
    CONSTRAINT FK_PhongTro_LoaiPhong FOREIGN KEY (LoaiPhongId) REFERENCES LOAIPHONG(LoaiPhongId),
    CONSTRAINT UQ_MaPhong_Building UNIQUE (MaPhong, BuildingId),
    CONSTRAINT CK_TrangThai CHECK (TrangThai IN (N'Trống', N'Đang thuê', N'Đang sửa'))
);
GO

-- =====================================================
-- 6. BẢNG LICHSU_GIA - Lịch sử thay đổi giá phòng
-- =====================================================
CREATE TABLE LICHSU_GIA (
    LichSuGiaId INT IDENTITY(1,1) PRIMARY KEY,
    PhongId INT NOT NULL,
    GiaCu DECIMAL(18,0),
    GiaMoi DECIMAL(18,0) NOT NULL,
    NgayApDung DATETIME NOT NULL,
    NgayKetThuc DATETIME,
    GhiChu NVARCHAR(255),
    CreatedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_LichSuGia_Phong FOREIGN KEY (PhongId) REFERENCES PHONGTRO(PhongId),
    CONSTRAINT FK_LichSuGia_User FOREIGN KEY (CreatedBy) REFERENCES USERS(UserId)
);
GO

-- =====================================================
-- 7. BẢNG TAISAN - Danh mục tài sản
-- =====================================================
CREATE TABLE TAISAN (
    TaiSanId INT IDENTITY(1,1) PRIMARY KEY,
    TenTaiSan NVARCHAR(100) NOT NULL,
    DonVi NVARCHAR(20) DEFAULT N'Cái',
    GiaTri DECIMAL(18,0) DEFAULT 0, -- Giá trị tài sản để tính khấu hao
    MoTa NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- =====================================================
-- 8. BẢNG TAISAN_PHONG - Tài sản trong từng phòng
-- =====================================================
CREATE TABLE TAISAN_PHONG (
    TaiSanPhongId INT IDENTITY(1,1) PRIMARY KEY,
    PhongId INT NOT NULL,
    TaiSanId INT NOT NULL,
    SoLuong INT DEFAULT 1,
    TinhTrang NVARCHAR(50) DEFAULT N'Tốt', -- Tốt, Hỏng, Đang sửa
    NgayNhap DATETIME DEFAULT GETDATE(),
    GhiChu NVARCHAR(255),
    CONSTRAINT FK_TaiSanPhong_Phong FOREIGN KEY (PhongId) REFERENCES PHONGTRO(PhongId),
    CONSTRAINT FK_TaiSanPhong_TaiSan FOREIGN KEY (TaiSanId) REFERENCES TAISAN(TaiSanId)
);
GO

-- =====================================================
-- 9. BẢNG KHACHTHUE - Khách thuê
-- =====================================================
CREATE TABLE KHACHTHUE (
    KhachId INT IDENTITY(1,1) PRIMARY KEY,
    MaKhach NVARCHAR(20) NOT NULL UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    CCCD NVARCHAR(20) NOT NULL UNIQUE,
    NgaySinh DATE,
    GioiTinh NVARCHAR(10),
    DiaChi NVARCHAR(255),
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    NgheNghiep NVARCHAR(100),
    NoiLamViec NVARCHAR(255),
    HinhAnh NVARCHAR(255),
    UserId INT, -- Liên kết với tài khoản đăng nhập (nếu có)
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT FK_KhachThue_User FOREIGN KEY (UserId) REFERENCES USERS(UserId)
);
GO

-- =====================================================
-- 10. BẢNG HOPDONG - Hợp đồng thuê
-- =====================================================
CREATE TABLE HOPDONG (
    HopDongId INT IDENTITY(1,1) PRIMARY KEY,
    MaHopDong NVARCHAR(20) NOT NULL UNIQUE,
    PhongId INT NOT NULL,
    KhachId INT NOT NULL,
    NgayBatDau DATE NOT NULL,
    NgayKetThuc DATE NOT NULL,
    GiaThue DECIMAL(18,0) NOT NULL, -- Giá thuê tại thời điểm ký HĐ
    TienCoc DECIMAL(18,0) DEFAULT 0,
    ChuKyThanhToan INT DEFAULT 1, -- Số tháng/kỳ thanh toán
    TrangThai NVARCHAR(20) DEFAULT N'Active', -- Active, Expired, Terminated
    NgayThanhLy DATE,
    TienHoanCoc DECIMAL(18,0),
    TienKhauTru DECIMAL(18,0),
    LyDoKhauTru NVARCHAR(500),
    GhiChu NVARCHAR(500),
    CreatedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT FK_HopDong_Phong FOREIGN KEY (PhongId) REFERENCES PHONGTRO(PhongId),
    CONSTRAINT FK_HopDong_Khach FOREIGN KEY (KhachId) REFERENCES KHACHTHUE(KhachId),
    CONSTRAINT FK_HopDong_User FOREIGN KEY (CreatedBy) REFERENCES USERS(UserId),
    CONSTRAINT CK_HopDong_TrangThai CHECK (TrangThai IN (N'Active', N'Expired', N'Terminated'))
);
GO

-- =====================================================
-- 11. BẢNG DICHVU - Dịch vụ (Điện, Nước, Wifi...)
-- =====================================================
CREATE TABLE DICHVU (
    DichVuId INT IDENTITY(1,1) PRIMARY KEY,
    MaDichVu NVARCHAR(20) NOT NULL UNIQUE,
    TenDichVu NVARCHAR(100) NOT NULL,
    DonGia DECIMAL(18,0) NOT NULL,
    DonViTinh NVARCHAR(20), -- kWh, m3, tháng...
    LoaiDichVu NVARCHAR(20) DEFAULT N'CoDinh', -- CoDinh (cố định), TheoChiSo (theo chỉ số)
    MoTa NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT CK_LoaiDichVu CHECK (LoaiDichVu IN (N'CoDinh', N'TheoChiSo'))
);
GO

-- =====================================================
-- 12. BẢNG HOADON - Hóa đơn hàng tháng
-- =====================================================
CREATE TABLE HOADON (
    HoaDonId INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon NVARCHAR(20) NOT NULL UNIQUE,
    HopDongId INT NOT NULL,
    ThangNam DATE NOT NULL, -- Tháng/Năm của hóa đơn (lưu ngày đầu tháng)
    TienPhong DECIMAL(18,0) NOT NULL,
    TongTienDichVu DECIMAL(18,0) DEFAULT 0,
    TongCong DECIMAL(18,0) NOT NULL,
    DaThanhToan DECIMAL(18,0) DEFAULT 0,
    ConNo DECIMAL(18,0),
    NgayTao DATE DEFAULT GETDATE(),
    NgayHetHan DATE,
    NgayThanhToan DATE,
    TrangThai NVARCHAR(20) DEFAULT N'ChuaThanhToan', -- ChuaThanhToan, DaThanhToan, QuaHan
    GhiChu NVARCHAR(500),
    CreatedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT FK_HoaDon_HopDong FOREIGN KEY (HopDongId) REFERENCES HOPDONG(HopDongId),
    CONSTRAINT FK_HoaDon_User FOREIGN KEY (CreatedBy) REFERENCES USERS(UserId),
    CONSTRAINT CK_HoaDon_TrangThai CHECK (TrangThai IN (N'ChuaThanhToan', N'DaThanhToan', N'QuaHan'))
);
GO

-- =====================================================
-- 13. BẢNG CHITIETHOADON - Chi tiết hóa đơn dịch vụ
-- =====================================================
CREATE TABLE CHITIETHOADON (
    ChiTietId INT IDENTITY(1,1) PRIMARY KEY,
    HoaDonId INT NOT NULL,
    DichVuId INT NOT NULL,
    ChiSoCu DECIMAL(18,2),
    ChiSoMoi DECIMAL(18,2),
    SoLuong DECIMAL(18,2),
    DonGia DECIMAL(18,0) NOT NULL,
    ThanhTien DECIMAL(18,0) NOT NULL,
    GhiChu NVARCHAR(255),
    CONSTRAINT FK_ChiTietHD_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HOADON(HoaDonId),
    CONSTRAINT FK_ChiTietHD_DichVu FOREIGN KEY (DichVuId) REFERENCES DICHVU(DichVuId)
);
GO

-- =====================================================
-- 14. BẢNG BAOTRI_TICKET - Yêu cầu bảo trì
-- =====================================================
CREATE TABLE BAOTRI_TICKET (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    MaTicket NVARCHAR(20) NOT NULL UNIQUE,
    PhongId INT NOT NULL,
    KhachId INT,
    TieuDe NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(1000),
    MucDoUuTien NVARCHAR(20) DEFAULT N'Trung bình', -- Thấp, Trung bình, Cao, Khẩn cấp
    TrangThai NVARCHAR(20) DEFAULT N'Mới', -- Mới, Đang xử lý, Hoàn thành, Hủy
    NgayTao DATETIME DEFAULT GETDATE(),
    NgayXuLy DATETIME,
    NgayHoanThanh DATETIME,
    NguoiXuLy INT,
    ChiPhiSuaChua DECIMAL(18,0) DEFAULT 0,
    KetQuaXuLy NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    CONSTRAINT FK_BaoTri_Phong FOREIGN KEY (PhongId) REFERENCES PHONGTRO(PhongId),
    CONSTRAINT FK_BaoTri_Khach FOREIGN KEY (KhachId) REFERENCES KHACHTHUE(KhachId),
    CONSTRAINT FK_BaoTri_NguoiXuLy FOREIGN KEY (NguoiXuLy) REFERENCES USERS(UserId),
    CONSTRAINT CK_MucDoUuTien CHECK (MucDoUuTien IN (N'Thấp', N'Trung bình', N'Cao', N'Khẩn cấp')),
    CONSTRAINT CK_BaoTri_TrangThai CHECK (TrangThai IN (N'Mới', N'Đang xử lý', N'Hoàn thành', N'Hủy'))
);
GO

-- =====================================================
-- 15. BẢNG NOTIFICATION_LOG - Thông báo
-- =====================================================
CREATE TABLE NOTIFICATION_LOG (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    LoaiThongBao NVARCHAR(50), -- HopDongSapHetHan, HoaDonQuaHan, BaoTriMoi, etc.
    TieuDe NVARCHAR(200) NOT NULL,
    NoiDung NVARCHAR(1000),
    DuongDan NVARCHAR(255), -- Link đến form/chức năng liên quan
    DaDoc BIT DEFAULT 0,
    NgayTao DATETIME DEFAULT GETDATE(),
    NgayDoc DATETIME,
    CONSTRAINT FK_Notification_User FOREIGN KEY (UserId) REFERENCES USERS(UserId)
);
GO

-- =====================================================
-- 16. BẢNG ACTIVITY_LOG - Nhật ký hoạt động
-- =====================================================
CREATE TABLE ACTIVITY_LOG (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    TenBang NVARCHAR(50), -- Tên bảng bị tác động
    MaBanGhi NVARCHAR(50), -- ID/Mã của bản ghi
    HanhDong NVARCHAR(20), -- INSERT, UPDATE, DELETE
    DuLieuCu NVARCHAR(MAX), -- JSON data cũ
    DuLieuMoi NVARCHAR(MAX), -- JSON data mới
    MoTa NVARCHAR(500),
    IpAddress NVARCHAR(50),
    NgayThucHien DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_ActivityLog_User FOREIGN KEY (UserId) REFERENCES USERS(UserId)
);
GO

-- =====================================================
-- 17. BẢNG CAUHINH - Cấu hình hệ thống
-- =====================================================
CREATE TABLE CAUHINH (
    CauHinhId INT IDENTITY(1,1) PRIMARY KEY,
    MaCauHinh NVARCHAR(50) NOT NULL UNIQUE,
    TenCauHinh NVARCHAR(100) NOT NULL,
    GiaTri NVARCHAR(500),
    LoaiDuLieu NVARCHAR(20) DEFAULT 'String', -- String, Int, Decimal, Boolean
    MoTa NVARCHAR(255),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- =====================================================
-- TẠO INDEX ĐỂ TỐI ƯU TRUY VẤN
-- =====================================================
CREATE INDEX IX_PhongTro_BuildingId ON PHONGTRO(BuildingId);
CREATE INDEX IX_PhongTro_TrangThai ON PHONGTRO(TrangThai);
CREATE INDEX IX_HopDong_PhongId ON HOPDONG(PhongId);
CREATE INDEX IX_HopDong_KhachId ON HOPDONG(KhachId);
CREATE INDEX IX_HopDong_TrangThai ON HOPDONG(TrangThai);
CREATE INDEX IX_HoaDon_HopDongId ON HOADON(HopDongId);
CREATE INDEX IX_HoaDon_TrangThai ON HOADON(TrangThai);
CREATE INDEX IX_HoaDon_ThangNam ON HOADON(ThangNam);
CREATE INDEX IX_Notification_UserId ON NOTIFICATION_LOG(UserId, DaDoc);
CREATE INDEX IX_ActivityLog_UserId ON ACTIVITY_LOG(UserId, NgayThucHien);
GO

-- =====================================================
-- INSERT DỮ LIỆU MẪU
-- =====================================================

-- Roles
INSERT INTO ROLES (RoleName, Description) VALUES
(N'Admin', N'Quản trị viên hệ thống'),
(N'Manager', N'Quản lý nhà trọ'),
(N'Tenant', N'Khách thuê');
GO

-- Users (Password: 123456 - đã hash bằng SHA256)
INSERT INTO USERS (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive) VALUES
(N'admin', N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Administrator', N'admin@nhatro.com', N'0123456789', 1, 1),
(N'manager1', N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Nguyễn Văn Quản Lý', N'manager@nhatro.com', N'0987654321', 2, 1),
(N'tenant1', N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Trần Văn Khách', N'khach1@email.com', N'0912345678', 3, 1);
GO

-- Building
INSERT INTO BUILDING (BuildingCode, BuildingName, Address, TotalFloors, Description) VALUES
(N'A', N'Tòa nhà A', N'123 Đường Nguyễn Văn A, Quận 1, TP.HCM', 5, N'Tòa nhà chính'),
(N'B', N'Tòa nhà B', N'456 Đường Lê Văn B, Quận 2, TP.HCM', 4, N'Tòa nhà phụ');
GO

-- Loại phòng
INSERT INTO LOAIPHONG (TenLoai, MoTa) VALUES
(N'Phòng đơn', N'Phòng cho 1-2 người'),
(N'Phòng đôi', N'Phòng cho 2-3 người'),
(N'Phòng gia đình', N'Phòng rộng cho gia đình'),
(N'Studio', N'Phòng có bếp riêng');
GO

-- Phòng trọ
INSERT INTO PHONGTRO (MaPhong, BuildingId, LoaiPhongId, Tang, DienTich, GiaThue, SoNguoiToiDa, TrangThai) VALUES
(N'A101', 1, 1, 1, 20, 3000000, 2, N'Trống'),
(N'A102', 1, 1, 1, 22, 3200000, 2, N'Trống'),
(N'A201', 1, 2, 2, 28, 4000000, 3, N'Trống'),
(N'A202', 1, 2, 2, 30, 4200000, 3, N'Trống'),
(N'A301', 1, 3, 3, 40, 5500000, 4, N'Trống'),
(N'B101', 2, 1, 1, 18, 2800000, 2, N'Trống'),
(N'B102', 2, 1, 1, 20, 3000000, 2, N'Trống'),
(N'B201', 2, 4, 2, 35, 4800000, 2, N'Trống');
GO

-- Tài sản
INSERT INTO TAISAN (TenTaiSan, DonVi, GiaTri, MoTa) VALUES
(N'Giường', N'Cái', 2000000, N'Giường ngủ đơn/đôi'),
(N'Tủ quần áo', N'Cái', 1500000, N'Tủ gỗ'),
(N'Bàn học', N'Cái', 800000, N'Bàn làm việc'),
(N'Ghế', N'Cái', 300000, N'Ghế ngồi'),
(N'Quạt trần', N'Cái', 500000, N'Quạt điện'),
(N'Điều hòa', N'Cái', 8000000, N'Máy lạnh'),
(N'Bình nóng lạnh', N'Cái', 3000000, N'Bình nước nóng'),
(N'Tủ lạnh', N'Cái', 4000000, N'Tủ lạnh mini');
GO

-- Tài sản trong phòng
INSERT INTO TAISAN_PHONG (PhongId, TaiSanId, SoLuong, TinhTrang) VALUES
(1, 1, 1, N'Tốt'), (1, 2, 1, N'Tốt'), (1, 5, 1, N'Tốt'),
(2, 1, 1, N'Tốt'), (2, 2, 1, N'Tốt'), (2, 6, 1, N'Tốt'),
(3, 1, 2, N'Tốt'), (3, 2, 1, N'Tốt'), (3, 6, 1, N'Tốt'),
(4, 1, 2, N'Tốt'), (4, 2, 1, N'Tốt'), (4, 6, 1, N'Tốt'), (4, 7, 1, N'Tốt');
GO

-- Dịch vụ
INSERT INTO DICHVU (MaDichVu, TenDichVu, DonGia, DonViTinh, LoaiDichVu, MoTa) VALUES
(N'DIEN', N'Tiền điện', 3500, N'kWh', N'TheoChiSo', N'Tính theo chỉ số điện'),
(N'NUOC', N'Tiền nước', 15000, N'm³', N'TheoChiSo', N'Tính theo chỉ số nước'),
(N'WIFI', N'Internet/Wifi', 100000, N'Tháng', N'CoDinh', N'Phí wifi hàng tháng'),
(N'RAC', N'Vệ sinh/Rác', 20000, N'Tháng', N'CoDinh', N'Phí vệ sinh'),
(N'GIU_XE', N'Giữ xe', 100000, N'Tháng', N'CoDinh', N'Phí giữ xe máy');
GO

-- Khách thuê mẫu
INSERT INTO KHACHTHUE (MaKhach, HoTen, CCCD, NgaySinh, GioiTinh, DiaChi, Phone, Email, NgheNghiep, UserId) VALUES
(N'KH001', N'Trần Văn Khách', N'012345678901', '1995-05-15', N'Nam', N'Quảng Ngãi', N'0912345678', N'khach1@email.com', N'Nhân viên văn phòng', 3),
(N'KH002', N'Nguyễn Thị Mai', N'012345678902', '1998-08-20', N'Nữ', N'Bình Định', N'0923456789', N'mai@email.com', N'Sinh viên', NULL),
(N'KH003', N'Lê Văn Hùng', N'012345678903', '1990-03-10', N'Nam', N'Đà Nẵng', N'0934567890', N'hung@email.com', N'Kỹ sư', NULL);
GO

-- Hợp đồng mẫu
INSERT INTO HOPDONG (MaHopDong, PhongId, KhachId, NgayBatDau, NgayKetThuc, GiaThue, TienCoc, TrangThai, CreatedBy) VALUES
(N'HD001', 1, 1, '2024-01-01', '2025-01-01', 3000000, 6000000, N'Active', 2),
(N'HD002', 3, 2, '2024-06-01', '2025-06-01', 4000000, 8000000, N'Active', 2);
GO

-- Cập nhật trạng thái phòng
UPDATE PHONGTRO SET TrangThai = N'Đang thuê' WHERE PhongId IN (1, 3);
GO

-- Cấu hình hệ thống
INSERT INTO CAUHINH (MaCauHinh, TenCauHinh, GiaTri, LoaiDuLieu, MoTa) VALUES
(N'TEN_NHA_TRO', N'Tên nhà trọ', N'Nhà Trọ ABC', N'String', N'Tên hiển thị của nhà trọ'),
(N'DIA_CHI', N'Địa chỉ', N'123 Đường ABC, TP.HCM', N'String', N'Địa chỉ nhà trọ'),
(N'SDT_LIEN_HE', N'Số điện thoại', N'0123456789', N'String', N'SĐT liên hệ'),
(N'EMAIL', N'Email', N'contact@nhatro.com', N'String', N'Email liên hệ'),
(N'GIA_DIEN_MAC_DINH', N'Giá điện mặc định', N'3500', N'Decimal', N'Giá điện mặc định (VND/kWh)'),
(N'GIA_NUOC_MAC_DINH', N'Giá nước mặc định', N'15000', N'Decimal', N'Giá nước mặc định (VND/m³)'),
(N'SMTP_HOST', N'SMTP Server', N'smtp.gmail.com', N'String', N'SMTP server gửi email'),
(N'SMTP_PORT', N'SMTP Port', N'587', N'Int', N'SMTP port'),
(N'SMTP_EMAIL', N'Email gửi', N'', N'String', N'Email dùng để gửi hóa đơn'),
(N'SMTP_PASSWORD', N'Mật khẩu email', N'', N'String', N'App password của Gmail'),
(N'NGAY_TAO_HOADON', N'Ngày tạo hóa đơn', N'1', N'Int', N'Ngày trong tháng tạo hóa đơn tự động'),
(N'SO_NGAY_HET_HAN', N'Số ngày hết hạn', N'10', N'Int', N'Số ngày từ khi tạo đến hạn thanh toán');
GO

-- =====================================================
-- STORED PROCEDURES
-- =====================================================

-- SP: Lấy danh sách phòng với thông tin chi tiết
CREATE PROCEDURE sp_GetPhongTroList
    @BuildingId INT = NULL,
    @TrangThai NVARCHAR(20) = NULL
AS
BEGIN
    SELECT p.*, b.BuildingName, b.BuildingCode, lp.TenLoai,
           hd.MaHopDong, hd.HopDongId, k.HoTen AS TenKhachThue
    FROM PHONGTRO p
    LEFT JOIN BUILDING b ON p.BuildingId = b.BuildingId
    LEFT JOIN LOAIPHONG lp ON p.LoaiPhongId = lp.LoaiPhongId
    LEFT JOIN HOPDONG hd ON p.PhongId = hd.PhongId AND hd.TrangThai = N'Active'
    LEFT JOIN KHACHTHUE k ON hd.KhachId = k.KhachId
    WHERE p.IsActive = 1
      AND (@BuildingId IS NULL OR p.BuildingId = @BuildingId)
      AND (@TrangThai IS NULL OR p.TrangThai = @TrangThai)
    ORDER BY b.BuildingCode, p.Tang, p.MaPhong;
END
GO

-- SP: Thống kê Dashboard
CREATE PROCEDURE sp_GetDashboardStats
AS
BEGIN
    -- Số phòng theo trạng thái
    SELECT
        COUNT(*) AS TongPhong,
        SUM(CASE WHEN TrangThai = N'Trống' THEN 1 ELSE 0 END) AS PhongTrong,
        SUM(CASE WHEN TrangThai = N'Đang thuê' THEN 1 ELSE 0 END) AS PhongDangThue,
        SUM(CASE WHEN TrangThai = N'Đang sửa' THEN 1 ELSE 0 END) AS PhongDangSua
    FROM PHONGTRO WHERE IsActive = 1;

    -- Số khách thuê đang hoạt động
    SELECT COUNT(DISTINCT KhachId) AS TongKhachThue
    FROM HOPDONG WHERE TrangThai = N'Active';

    -- Doanh thu tháng này
    SELECT ISNULL(SUM(DaThanhToan), 0) AS DoanhThuThang
    FROM HOADON
    WHERE MONTH(NgayThanhToan) = MONTH(GETDATE())
      AND YEAR(NgayThanhToan) = YEAR(GETDATE());

    -- Tổng công nợ
    SELECT ISNULL(SUM(ConNo), 0) AS TongCongNo
    FROM HOADON WHERE TrangThai != N'DaThanhToan';

    -- Doanh thu 12 tháng gần nhất
    SELECT
        FORMAT(ThangNam, 'MM/yyyy') AS Thang,
        SUM(DaThanhToan) AS DoanhThu
    FROM HOADON
    WHERE ThangNam >= DATEADD(MONTH, -12, GETDATE())
    GROUP BY FORMAT(ThangNam, 'MM/yyyy'), ThangNam
    ORDER BY ThangNam;
END
GO

-- SP: Tạo hóa đơn tự động cho tháng
CREATE PROCEDURE sp_TaoHoaDonThang
    @ThangNam DATE,
    @CreatedBy INT
AS
BEGIN
    DECLARE @MaHoaDon NVARCHAR(20);
    DECLARE @HopDongId INT, @GiaThue DECIMAL(18,0);

    -- Cursor qua các hợp đồng active
    DECLARE hd_cursor CURSOR FOR
    SELECT HopDongId, GiaThue FROM HOPDONG WHERE TrangThai = N'Active';

    OPEN hd_cursor;
    FETCH NEXT FROM hd_cursor INTO @HopDongId, @GiaThue;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Kiểm tra đã có hóa đơn tháng này chưa
        IF NOT EXISTS (SELECT 1 FROM HOADON WHERE HopDongId = @HopDongId AND ThangNam = @ThangNam)
        BEGIN
            SET @MaHoaDon = 'HD' + FORMAT(@ThangNam, 'yyyyMM') + RIGHT('000' + CAST(@HopDongId AS NVARCHAR), 3);

            INSERT INTO HOADON (MaHoaDon, HopDongId, ThangNam, TienPhong, TongCong, ConNo, NgayHetHan, TrangThai, CreatedBy)
            VALUES (@MaHoaDon, @HopDongId, @ThangNam, @GiaThue, @GiaThue, @GiaThue,
                    DATEADD(DAY, 10, @ThangNam), N'ChuaThanhToan', @CreatedBy);
        END

        FETCH NEXT FROM hd_cursor INTO @HopDongId, @GiaThue;
    END

    CLOSE hd_cursor;
    DEALLOCATE hd_cursor;
END
GO

-- SP: Lấy thông báo chưa đọc
CREATE PROCEDURE sp_GetNotifications
    @UserId INT
AS
BEGIN
    SELECT * FROM NOTIFICATION_LOG
    WHERE (UserId = @UserId OR UserId IS NULL) AND DaDoc = 0
    ORDER BY NgayTao DESC;
END
GO

-- SP: Kiểm tra và tạo thông báo tự động
CREATE PROCEDURE sp_CreateAutoNotifications
AS
BEGIN
    -- Thông báo hợp đồng sắp hết hạn (7 ngày)
    INSERT INTO NOTIFICATION_LOG (UserId, LoaiThongBao, TieuDe, NoiDung)
    SELECT NULL, N'HopDongSapHetHan',
           N'Hợp đồng sắp hết hạn: ' + h.MaHopDong,
           N'Hợp đồng ' + h.MaHopDong + ' của phòng ' + p.MaPhong + ' sẽ hết hạn vào ' + FORMAT(h.NgayKetThuc, 'dd/MM/yyyy')
    FROM HOPDONG h
    JOIN PHONGTRO p ON h.PhongId = p.PhongId
    WHERE h.TrangThai = N'Active'
      AND DATEDIFF(DAY, GETDATE(), h.NgayKetThuc) <= 7
      AND NOT EXISTS (
          SELECT 1 FROM NOTIFICATION_LOG
          WHERE LoaiThongBao = N'HopDongSapHetHan'
            AND NoiDung LIKE '%' + h.MaHopDong + '%'
            AND CAST(NgayTao AS DATE) = CAST(GETDATE() AS DATE)
      );

    -- Thông báo hóa đơn quá hạn
    INSERT INTO NOTIFICATION_LOG (UserId, LoaiThongBao, TieuDe, NoiDung)
    SELECT NULL, N'HoaDonQuaHan',
           N'Hóa đơn quá hạn: ' + hd.MaHoaDon,
           N'Hóa đơn ' + hd.MaHoaDon + ' đã quá hạn thanh toán. Số tiền còn nợ: ' + FORMAT(hd.ConNo, 'N0') + ' VNĐ'
    FROM HOADON hd
    WHERE hd.TrangThai = N'ChuaThanhToan'
      AND hd.NgayHetHan < GETDATE()
      AND NOT EXISTS (
          SELECT 1 FROM NOTIFICATION_LOG
          WHERE LoaiThongBao = N'HoaDonQuaHan'
            AND NoiDung LIKE '%' + hd.MaHoaDon + '%'
            AND CAST(NgayTao AS DATE) = CAST(GETDATE() AS DATE)
      );
END
GO

PRINT N'Database QuanLyNhaTro created successfully!';
GO
