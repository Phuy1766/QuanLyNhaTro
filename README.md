# Quản Lý Nhà Trọ Pro 2025

Hệ thống quản lý nhà trọ chuyên nghiệp được xây dựng với WinForms C# .NET 8

## Tính năng chính

### Theo Actor
- **Admin**: Quản lý tài khoản, cấu hình hệ thống, backup/restore, báo cáo tổng hợp
- **Manager**: CRUD phòng, khách, hợp đồng, hóa đơn, dịch vụ, bảo trì
- **Tenant**: Xem thông tin cá nhân, hóa đơn, gửi yêu cầu bảo trì

### Chức năng nâng cao
- Dashboard thống kê realtime
- Dark/Light Theme Toggle
- Gửi hóa đơn qua email (SMTP Gmail)
- Quản lý tài sản phòng
- Lịch sử giá động
- Thanh lý hợp đồng với tính cọc tự động
- Notification Center
- Activity Log
- **Thuê phòng online với thanh toán QR VietQR** ⭐ NEW
  - Tenant tìm phòng trống và gửi yêu cầu thuê
  - Hệ thống tự động tạo mã QR thanh toán cọc
  - Sử dụng API VietQR chuẩn (img.vietqr.io)
  - Admin xác nhận thanh toán và duyệt hợp đồng

## Cấu trúc dự án

```
QuanLyNhaTro/
├── QuanLyNhaTro.sln              # Solution file
├── Database/
│   └── CreateDatabase.sql        # Script tạo database
├── QuanLyNhaTro.DAL/             # Data Access Layer
│   ├── Models/                   # Entity classes
│   ├── Repositories/             # Repository pattern
│   └── DatabaseHelper.cs
├── QuanLyNhaTro.BLL/             # Business Logic Layer
│   ├── Services/                 # Business services
│   └── Helpers/                  # Validation, Password
└── QuanLyNhaTro.UI/              # WinForms UI
    ├── Forms/                    # Main forms
    ├── UserControls/             # CRUD screens
    ├── Themes/                   # Theme manager
    └── Helpers/                  # UI utilities
```

## Yêu cầu hệ thống

- Windows 10/11
- .NET 8.0 SDK
- SQL Server 2019+ (hoặc SQL Server Express/LocalDB)
- Visual Studio 2022

## Cài đặt

### 1. Clone/Download project

### 2. Tạo Database
Mở SQL Server Management Studio và chạy script:
```
Database/CreateDatabase.sql
```

### 3. Cấu hình Connection String
Mở file `QuanLyNhaTro.UI/Program.cs` và sửa:
```csharp
DatabaseHelper.Initialize(
    server: ".\\SQLEXPRESS",  // Thay bằng server của bạn
    database: "QuanLyNhaTro",
    integratedSecurity: true
);
```

### 4. Build và Run
```bash
dotnet restore
dotnet build
dotnet run --project QuanLyNhaTro.UI
```

Hoặc mở Solution trong Visual Studio và nhấn F5.

## Tài khoản mặc định

| Username | Password | Role    |
|----------|----------|---------|
| admin    | 123456   | Admin   |
| manager1 | 123456   | Manager |
| tenant1  | 123456   | Tenant  |

## Database Schema (20 bảng)

1. **ROLES** - Vai trò người dùng
2. **USERS** - Tài khoản
3. **BUILDING** - Tòa nhà
4. **LOAIPHONG** - Loại phòng
5. **PHONGTRO** - Phòng trọ
6. **LICHSU_GIA** - Lịch sử giá phòng
7. **TAISAN** - Danh mục tài sản
8. **TAISAN_PHONG** - Tài sản trong phòng
9. **KHACHTHUE** - Khách thuê
10. **HOPDONG** - Hợp đồng thuê
11. **DICHVU** - Dịch vụ
12. **HOADON** - Hóa đơn
13. **CHITIETHOADON** - Chi tiết hóa đơn
14. **BAOTRI_TICKET** - Yêu cầu bảo trì
15. **NOTIFICATION_LOG** - Thông báo
16. **ACTIVITY_LOG** - Nhật ký hoạt động
17. **CAUHINH** - Cấu hình hệ thống
18. **YEUCAU_THUEPHONG** - Yêu cầu thuê phòng (booking)
19. **PAYMENT_CONFIG** - Cấu hình thanh toán ngân hàng
20. **BOOKING_PAYMENT** - Lịch sử thanh toán cọc

## Packages sử dụng

```xml
<!-- DAL -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.2" />
<PackageReference Include="Dapper" Version="2.1.24" />

<!-- UI -->
<PackageReference Include="Guna.UI2.WinForms" Version="2.0.4.6" />
<PackageReference Include="LiveChartsCore.SkiaSharpView.WinForms" Version="2.0.0-rc2" />
<PackageReference Include="EPPlus" Version="7.0.0" />
<PackageReference Include="iTextSharp.LGPLv2.Core" Version="3.4.20" />
<PackageReference Include="QRCoder" Version="1.6.0" />
```

## Cấu hình thanh toán VietQR

### 1. Thêm cấu hình ngân hàng (Admin)
1. Đăng nhập với tài khoản **Admin**
2. Vào menu **Quản lý** > **Cấu hình thanh toán**
3. Thêm thông tin tài khoản ngân hàng:
   - **Tên ngân hàng**: VD: Vietcombank
   - **Mã ngân hàng**: VD: VCB, MB, TCB... (xem danh sách trong code)
   - **Số tài khoản**: Số tài khoản thật của bạn
   - **Chủ tài khoản**: Tên chủ tài khoản
   - **Số tháng cọc**: Mặc định 1 (1 tháng tiền phòng)
   - **Template nội dung CK**: `NTPRO_{MaYeuCau}_{MaPhong}` (tùy chỉnh)

### 2. Danh sách mã ngân hàng hỗ trợ
```
VCB      - Vietcombank     MB       - MB Bank
TCB      - Techcombank     BIDV     - BIDV
VPB      - VPBank          VIB      - VIB
ACB      - ACB             TPB      - TPBank
STB      - Sacombank       VTB      - VietinBank
... và 30+ ngân hàng khác
```

### 3. Quy trình thuê phòng online
**Bước 1 - Tenant tìm phòng:**
1. Đăng nhập với tài khoản Tenant
2. Vào **Tìm phòng trống**
3. Chọn phòng và nhấn **Thuê phòng**
4. Điền thông tin: Ngày bắt đầu, Số người, Ghi chú
5. Hệ thống hiển thị popup QR thanh toán

**Bước 2 - Thanh toán QR:**
1. Quét mã QR bằng app ngân hàng (VietQR)
2. Kiểm tra thông tin: STK, số tiền, nội dung CK
3. Chuyển khoản
4. Quay lại ứng dụng và nhấn **Tôi đã thanh toán**

**Bước 3 - Admin xác nhận:**
1. Đăng nhập với tài khoản Admin/Manager
2. Vào **Yêu cầu thuê phòng**
3. Filter "Chờ xác nhận TT"
4. Kiểm tra giao dịch trong ngân hàng
5. Nhấn **Xác nhận đã nhận tiền**

**Bước 4 - Admin duyệt hợp đồng:**
1. Filter "Chờ duyệt HĐ"
2. Chọn yêu cầu và nhấn **Duyệt & Tạo HĐ**
3. Điền thông tin hợp đồng: Mã HĐ, Ngày kết thúc, Ghi chú
4. Hệ thống tự động tạo hợp đồng và cập nhật trạng thái phòng

### 4. VietQR API
Ứng dụng sử dụng **img.vietqr.io** - API miễn phí tạo QR thanh toán chuẩn:
- Không cần đăng ký tài khoản
- QR code được tạo từ server VietQR (không lưu local)
- Hỗ trợ 40+ ngân hàng Việt Nam
- Format chuẩn EMVCo, quét được trên mọi app banking

**Cấu trúc URL:**
```
https://img.vietqr.io/image/{BANK_BIN}-{ACCOUNT_NO}-compact2.png?amount={AMOUNT}&addInfo={DESCRIPTION}&accountName={ACCOUNT_NAME}
```

**Ví dụ:**
```
https://img.vietqr.io/image/970436-1234567890-compact2.png?amount=3000000&addInfo=NTPRO_5_P101&accountName=NGUYEN%20VAN%20A
```

## Cấu hình SMTP (Gửi email)

1. Vào Google Account > Security > App passwords
2. Tạo App password cho "Mail"
3. Trong ứng dụng, vào **Cài đặt** > nhập:
   - SMTP Host: `smtp.gmail.com`
   - SMTP Port: `587`
   - Email: `your-email@gmail.com`
   - App Password: `xxxx xxxx xxxx xxxx`

## Screenshots

### Login
- Form đăng nhập với giao diện 2 panel
- Validation real-time

### Dashboard
- 6 stat cards: Tổng phòng, Phòng trống, Khách thuê, Doanh thu, Công nợ, HĐ sắp hết hạn
- Danh sách hợp đồng sắp hết hạn
- Danh sách hóa đơn quá hạn

### Quản lý Phòng
- Filter theo tòa nhà, trạng thái
- DataGridView với màu theo trạng thái
- Popup form Add/Edit

### Hợp đồng
- Tạo hợp đồng mới
- Gia hạn hợp đồng
- Thanh lý với tính cọc tự động

### Hóa đơn
- Tạo hóa đơn đơn lẻ hoặc hàng loạt
- Nhập chỉ số điện nước
- Thanh toán
- Gửi email

## License

MIT License - Free for educational and commercial use.

## Author

Generated with Claude Code - Anthropic 2025
