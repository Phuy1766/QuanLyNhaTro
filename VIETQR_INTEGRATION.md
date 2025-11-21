# Tích hợp VietQR Payment - Hướng dẫn chi tiết

## Tổng quan
Tính năng thanh toán QR VietQR được tích hợp vào hệ thống **Quản lý nhà trọ Pro** cho phép:
- Tenant thuê phòng online với thanh toán cọc qua QR
- Admin xác nhận thanh toán và duyệt hợp đồng
- Sử dụng API VietQR chuẩn (img.vietqr.io) - miễn phí, không cần đăng ký

---

## 1. Các file đã tạo/sửa

### A. Database Scripts
#### `Database/03_BookingAndNotification.sql` (Modified)
- Bảng `YEUCAU_THUEPHONG` - Yêu cầu thuê phòng
- Fixed FK constraints: `PhongId` thay vì `MaPhong`
- Stored procedures: `sp_CreateYeuCau`, `sp_ApproveYeuCau`, `sp_RejectYeuCau`

#### `Database/04_PaymentQR.sql` (New)
**Tables:**
- `PAYMENT_CONFIG` - Cấu hình tài khoản ngân hàng
  - ConfigId, BankName, BankCode, AccountNumber, AccountName
  - TransferTemplate, DepositMonths, IsActive

- `BOOKING_PAYMENT` - Lịch sử thanh toán cọc
  - MaThanhToan, MaYeuCau, SoTien, NoiDungChuyenKhoan
  - TrangThai (Pending, WaitingConfirm, Paid, Canceled)
  - NgayTao, NgayTenantXacNhan, NgayAdminXacNhan

**Stored Procedures:**
- `sp_CreateBookingWithPayment` - Tạo yêu cầu + thanh toán
- `sp_ConfirmPaymentByTenant` - Tenant xác nhận đã chuyển khoản
- `sp_AdminConfirmPayment` - Admin xác nhận đã nhận tiền
- `sp_GetAllBookingRequests` - Lấy danh sách yêu cầu (join payment info)

---

### B. Data Access Layer (DAL)

#### `QuanLyNhaTro.DAL/Models/PaymentModels.cs` (New)
```csharp
public class PaymentConfig
{
    public int ConfigId { get; set; }
    public string BankName { get; set; }
    public string BankCode { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string TransferTemplate { get; set; }
    public int DepositMonths { get; set; }
    public bool IsActive { get; set; }
}

public class BookingPayment
{
    public int MaThanhToan { get; set; }
    public int MaYeuCau { get; set; }
    public decimal SoTien { get; set; }
    public string NoiDungChuyenKhoan { get; set; }
    public string TrangThai { get; set; }
    public string KieuThanhToan { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayTenantXacNhan { get; set; }
    public DateTime? NgayAdminXacNhan { get; set; }
}

public class BookingRequestDTO
{
    // Kết hợp YEUCAU_THUEPHONG + BOOKING_PAYMENT
    public int MaYeuCau { get; set; }
    public string TenTenant { get; set; }
    public string MaPhong { get; set; }
    public decimal? GiaPhong { get; set; }
    public decimal? SoTienCoc { get; set; }
    public string TrangThaiThanhToan { get; set; }
    public string TrangThai { get; set; }
    // ... các field khác
}
```

#### `QuanLyNhaTro.DAL/Repositories/PaymentRepository.cs` (New)
```csharp
public class PaymentRepository
{
    // Payment Config
    Task<IEnumerable<PaymentConfig>> GetAllConfigsAsync()
    Task<PaymentConfig?> GetDefaultConfigAsync()

    // Booking + Payment
    Task<CreateBookingResult> CreateBookingWithPaymentAsync(...)
    Task<BookingPayment?> GetPaymentByYeuCauAsync(int maYeuCau)
    Task<(bool, string)> ConfirmPaymentByTenantAsync(int maThanhToan, int userId)
    Task<(bool, string)> AdminConfirmPaymentAsync(int maThanhToan, int adminId, bool isApproved, string note)

    // List & Statistics
    Task<IEnumerable<BookingRequestDTO>> GetAllBookingRequestsAsync(string? trangThai = null)
    Task<(int WaitingConfirm, int PendingApprove)> CountPendingRequestsAsync()
}
```

#### `QuanLyNhaTro.DAL/Models/YeuCauThuePhong.cs` (Modified)
- Thêm property `PhongId` (int)
- `MaPhong` giữ lại cho hiển thị (nullable string)

#### `QuanLyNhaTro.DAL/Repositories/YeuCauThuePhongRepository.cs` (Modified)
- Đổi tất cả FK từ `MaPhong` sang `PhongId`
- Sửa `TenLoaiPhong` thành `TenLoai`
- Update JOINs với BUILDING table

---

### C. User Interface (UI)

#### `QuanLyNhaTro.UI/Helpers/QRCodeHelper.cs` (New)
**Chức năng chính:**
```csharp
public static class QRCodeHelper
{
    // Dictionary mapping 40+ ngân hàng Việt Nam
    public static readonly Dictionary<string, string> BankBins;

    // VietQR API Integration
    string GetVietQRImageUrl(bankBin, accountNumber, amount, description, accountName)
    Task<Bitmap?> GetVietQRImageAsync(...)
    Task<Bitmap?> GetVietQRImageByBankCodeAsync(bankCode, ...) // VCB -> 970436

    // Fallback - Offline QR
    Bitmap GenerateQRCode(content, pixelsPerModule)

    // Helper
    PaymentQRInfo CreatePaymentInfo(...)
}
```

**Format URL VietQR:**
```
https://img.vietqr.io/image/{BANK_BIN}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={AMOUNT}&addInfo={DESC}&accountName={NAME}
```

**Ví dụ:**
```csharp
var bankCode = "VCB"; // Vietcombank
var bankBin = QRCodeHelper.GetBankBin(bankCode); // 970436
var url = QRCodeHelper.GetVietQRImageUrl(
    bankBin: "970436",
    accountNumber: "1234567890",
    amount: 3000000,
    description: "NTPRO_5_P101",
    accountName: "NGUYEN VAN A"
);
// => https://img.vietqr.io/image/970436-1234567890-compact2.png?amount=3000000&addInfo=NTPRO_5_P101&accountName=NGUYEN%20VAN%20A

var qrBitmap = await QRCodeHelper.GetVietQRImageByBankCodeAsync("VCB", "1234567890", 3000000, "NTPRO_5_P101", "NGUYEN VAN A");
```

#### `QuanLyNhaTro.UI/UserControls/ucAvailableRooms.cs` (Rewritten)
**Redesigned UI:**
- Card-based layout với FlowLayoutPanel
- Modern filters: Building, Floor, Price range, Max occupants
- Room cards with room info + "Thuê phòng" button

**Booking Flow:**
1. Click "Thuê phòng" → Popup form booking
2. Fill: Ngày bắt đầu, Số người, Ghi chú → Submit
3. Popup QR Payment:
   - Load QR từ VietQR API (async)
   - Hiển thị: Ngân hàng, STK, Số tiền, Nội dung CK
   - Nút: "Tôi đã thanh toán", "Hủy"

**Code snippet - QR Loading:**
```csharp
// Load QR asynchronously from VietQR API
_ = Task.Run(async () =>
{
    var qrBitmap = await QRCodeHelper.GetVietQRImageByBankCodeAsync(
        bankCode, accountNumber, amount, transferContent, accountName
    );

    picQR.Invoke(() => {
        if (qrBitmap != null)
            picQR.Image = qrBitmap;
        else
            picQR.Image = fallbackQR; // Offline QR
    });
});
```

#### `QuanLyNhaTro.UI/UserControls/ucBookingRequests.cs` (Completely Rewritten)
**Admin page - Xác nhận thanh toán:**

**New Features:**
1. **Columns:**
   - Giá phòng, Tiền cọc
   - Trạng thái thanh toán (TT Thanh toán)
   - Trạng thái yêu cầu (Trạng thái)

2. **Action Buttons Panel:**
   - 💵 **Xác nhận đã nhận tiền** (Enabled when WaitingConfirm)
   - ✗ **Hủy giao dịch** (Enabled when WaitingConfirm)
   - ✓ **Duyệt & Tạo HĐ** (Enabled when PendingApprove)
   - ✗ **Từ chối** (Enabled when PendingPayment/WaitingConfirm)

3. **Filter Options:**
   - Tất cả
   - Chờ xác nhận (Admin)
   - Chờ thanh toán
   - Chờ xác nhận TT
   - Chờ duyệt HĐ
   - Đã duyệt
   - Đã từ chối

4. **Status Flow:**
```
PendingPayment (Tenant chưa TT)
    ↓ Tenant: "Tôi đã thanh toán"
WaitingConfirm (Chờ Admin xác nhận)
    ↓ Admin: "Xác nhận đã nhận tiền"
PendingApprove (Chờ duyệt HĐ)
    ↓ Admin: "Duyệt & Tạo HĐ"
Approved (Hoàn tất)
```

**Code snippet - Admin confirm:**
```csharp
private async void BtnConfirmPayment_Click(object? sender, EventArgs e)
{
    var request = dgvRequests.SelectedRows[0].DataBoundItem as BookingRequestDTO;

    var (success, message) = await _paymentRepo.AdminConfirmPaymentAsync(
        request.MaThanhToan.Value,
        AuthService.CurrentUser?.UserId ?? 0,
        isApproved: true,
        note: "Admin xác nhận đã nhận tiền cọc"
    );

    if (success)
        UIHelper.ShowSuccess("Đã xác nhận thanh toán!");
}
```

---

## 2. Quy trình sử dụng

### Bước 1: Setup Database
```sql
-- Chạy lần lượt:
1. Database/CreateDatabase.sql (nếu chưa có)
2. Database/03_BookingAndNotification.sql
3. Database/04_PaymentQR.sql
```

### Bước 2: Cấu hình Payment (Admin)
```sql
-- Thêm config ngân hàng vào PAYMENT_CONFIG
INSERT INTO PAYMENT_CONFIG (BankName, BankCode, AccountNumber, AccountName, TransferTemplate, DepositMonths, IsActive)
VALUES (
    N'Vietcombank',
    'VCB',
    '1234567890',
    N'NGUYEN VAN A',
    'NTPRO_{MaYeuCau}_{MaPhong}',
    1,
    1
);
```

Hoặc qua UI: Menu **Quản lý** > **Cấu hình thanh toán** (cần implement ucPaymentConfig)

### Bước 3: Tenant thuê phòng
1. Đăng nhập Tenant
2. **Tìm phòng trống**
3. Click **Thuê phòng**
4. Điền form → Submit
5. Popup QR → Quét QR bằng app banking → Chuyển khoản
6. Click **Tôi đã thanh toán**

### Bước 4: Admin xác nhận
1. Đăng nhập Admin/Manager
2. **Yêu cầu thuê phòng**
3. Filter: "Chờ xác nhận TT"
4. Kiểm tra giao dịch trong ngân hàng
5. Chọn yêu cầu → **Xác nhận đã nhận tiền**

### Bước 5: Admin duyệt HĐ
1. Filter: "Chờ duyệt HĐ"
2. Chọn yêu cầu → **Duyệt & Tạo HĐ**
3. Điền: Mã HĐ, Ngày KT, Ghi chú
4. Hệ thống tự động:
   - Tạo HOPDONG
   - Tạo KHACHTHUE (nếu chưa có)
   - Update PHONGTRO.TrangThai = 'Occupied'
   - Update YEUCAU_THUEPHONG.TrangThai = 'Approved'
   - Update BOOKING_PAYMENT.TrangThai = 'Paid'

---

## 3. VietQR API Documentation

### API Endpoint
```
GET https://img.vietqr.io/image/{BANK_BIN}-{ACCOUNT_NO}-{TEMPLATE}.png
```

### Parameters
| Parameter | Description | Required |
|-----------|-------------|----------|
| BANK_BIN | Mã BIN ngân hàng (VD: 970436 = VCB) | Yes |
| ACCOUNT_NO | Số tài khoản | Yes |
| TEMPLATE | Template hiển thị: `compact`, `compact2`, `print`, `qr_only` | Yes |
| amount | Số tiền (VND) | No |
| addInfo | Nội dung chuyển khoản | No |
| accountName | Tên chủ tài khoản | No |

### Response
- **Success**: Image PNG (QR code)
- **Error**: 404 or Error image

### Supported Banks (40+)
```
VCB      - Vietcombank       (970436)
TCB      - Techcombank       (970407)
MB       - MB Bank           (970422)
VPB      - VPBank            (970432)
ACB      - ACB               (970416)
BIDV     - BIDV              (970418)
VTB      - VietinBank        (970415)
TPB      - TPBank            (970423)
STB      - Sacombank         (970403)
... và 30+ ngân hàng khác
```

### Example Requests
```
# Vietcombank - Có số tiền + nội dung
https://img.vietqr.io/image/970436-1234567890-compact2.png?amount=3000000&addInfo=NTPRO_5_P101&accountName=NGUYEN%20VAN%20A

# MB Bank - Không số tiền
https://img.vietqr.io/image/970422-9876543210-compact2.png?addInfo=Thanh%20toan%20phong&accountName=TRAN%20THI%20B

# TPBank - QR only (không thông tin bank)
https://img.vietqr.io/image/970423-1111222233-qr_only.png?amount=5000000
```

### Templates
- **compact**: QR + thông tin ngắn gọn
- **compact2**: QR + thông tin chi tiết (recommended)
- **print**: Định dạng in ấn
- **qr_only**: Chỉ mã QR, không logo/text

---

## 4. Danh sách mã BIN ngân hàng

```csharp
public static readonly Dictionary<string, string> BankBins = new()
{
    { "VCB", "970436" },      // Vietcombank
    { "TCB", "970407" },      // Techcombank
    { "MB", "970422" },       // MB Bank
    { "VPB", "970432" },      // VPBank
    { "ACB", "970416" },      // ACB
    { "TPB", "970423" },      // TPBank
    { "STB", "970403" },      // Sacombank
    { "BIDV", "970418" },     // BIDV
    { "VIB", "970441" },      // VIB
    { "SHB", "970443" },      // SHB
    { "EIB", "970431" },      // Eximbank
    { "MSB", "970426" },      // MSB
    { "HDB", "970437" },      // HDBank
    { "OCB", "970448" },      // OCB
    { "SCB", "970429" },      // SCB
    { "VTB", "970415" },      // VietinBank
    { "CAKE", "546034" },     // CAKE by VPBank
    { "UBANK", "546035" },    // Ubank by VPBank
    { "TIMO", "963388" },     // Timo by Ban Viet
    { "VNPTMONEY", "971011" }, // VNPT Money
    { "NAB", "970428" },      // Nam A Bank
    { "NCB", "970419" },      // NCB
    { "VIETBANK", "970433" }, // VietBank
    { "ABBANK", "970425" },   // ABBank
    { "BAB", "970409" },      // BacABank
    { "VBSP", "999888" },     // VBSP
    { "WOO", "970457" },      // Woori Bank
    { "KLB", "970452" },      // KienLongBank
    { "LPB", "970449" },      // LPBank
    { "SEAB", "970440" },     // SeABank
    { "CBB", "970444" },      // CBBank
    { "PGB", "970430" },      // PGBank
    { "PVCB", "970412" },     // PVcomBank
    { "OJB", "970414" },      // OceanBank
    { "GPB", "970408" },      // GPBank
    { "VARB", "999889" },     // Agribank
    { "SAIGONBANK", "970400" }, // Saigon Bank
};
```

---

## 5. Testing Guide

### Test Case 1: Tenant thuê phòng thành công
1. Login as tenant1/123456
2. Vào "Tìm phòng trống"
3. Chọn phòng → Thuê phòng
4. Điền thông tin → Submit
5. **Expected**: Popup QR hiển thị, QR load từ VietQR API
6. Click "Tôi đã thanh toán"
7. **Expected**: Success message, trạng thái chuyển sang WaitingConfirm

### Test Case 2: Admin xác nhận thanh toán
1. Login as admin/123456
2. Vào "Yêu cầu thuê phòng"
3. Filter "Chờ xác nhận TT"
4. Chọn yêu cầu vừa tạo
5. **Expected**: Nút "Xác nhận đã nhận tiền" enabled
6. Click xác nhận
7. **Expected**: Success, trạng thái chuyển sang PendingApprove

### Test Case 3: Admin duyệt hợp đồng
1. Filter "Chờ duyệt HĐ"
2. Chọn yêu cầu → "Duyệt & Tạo HĐ"
3. Điền Mã HĐ, Ngày KT
4. **Expected**: Hợp đồng được tạo, phòng chuyển sang Occupied

### Test Case 4: Admin hủy giao dịch
1. Login as tenant1, tạo yêu cầu mới
2. Click "Tôi đã thanh toán"
3. Login as admin
4. Chọn yêu cầu → "Hủy giao dịch"
5. Nhập lý do hủy
6. **Expected**: Payment.TrangThai = Canceled, YeuCau.TrangThai = Rejected

### Test Case 5: Không có internet (Fallback)
1. Ngắt kết nối internet
2. Tenant tạo yêu cầu thuê phòng
3. **Expected**: Popup QR vẫn hiển thị với offline QR (QRCoder)

---

## 6. Troubleshooting

### Lỗi: QR không load
**Nguyên nhân:**
- Không có internet
- VietQR API down
- Sai mã BIN

**Giải pháp:**
- Kiểm tra internet
- Xem console log: `System.Diagnostics.Debug.WriteLine`
- Hệ thống tự động fallback về offline QR

### Lỗi: Foreign key constraint
**Nguyên nhân:** Chạy sai thứ tự SQL scripts

**Giải pháp:**
```sql
-- Chạy lại đúng thứ tự:
1. CreateDatabase.sql
2. 03_BookingAndNotification.sql
3. 04_PaymentQR.sql
```

### Lỗi: Invalid column 'MaPhong'
**Nguyên nhân:** Stored procedures cũ vẫn dùng MaPhong thay vì PhongId

**Giải pháp:**
```sql
-- Drop và tạo lại stored procedures
DROP PROCEDURE IF EXISTS sp_CreateYeuCau;
-- Chạy lại script 03_BookingAndNotification.sql
```

### Lỗi: Nút "Xác nhận đã nhận tiền" không enable
**Nguyên nhân:**
- Sai trạng thái
- Chưa có MaThanhToan

**Giải pháp:**
- Kiểm tra: `TrangThai = 'WaitingConfirm'` và `TrangThaiThanhToan = 'WaitingConfirm'`
- Kiểm tra: `MaThanhToan IS NOT NULL`

---

## 7. Future Enhancements

### 7.1 Auto Payment Verification (Webhook)
Hiện tại: Admin phải xác nhận manual
Cải tiến: Tích hợp webhook từ ngân hàng để tự động xác nhận

### 7.2 Payment History Page
Trang xem lịch sử thanh toán của Tenant

### 7.3 Multiple Payment Methods
Thêm: Momo, ZaloPay, VNPay

### 7.4 Email Notifications
Gửi email khi:
- Tenant tạo yêu cầu
- Admin xác nhận/từ chối thanh toán
- Hợp đồng được duyệt

### 7.5 Admin Payment Config UI
Trang CRUD cho PAYMENT_CONFIG (hiện tại phải INSERT manual)

### 7.6 QR Code with Logo
Thêm logo app vào giữa QR code

### 7.7 Payment Analytics Dashboard
Thống kê: Tổng tiền cọc, Success rate, Avg processing time

---

## 8. Security Notes

### 8.1 SQL Injection Prevention
- Sử dụng Dapper parameterized queries
- Không concat string trong SQL

### 8.2 Authentication
- Kiểm tra `AuthService.CurrentUser` trước khi thực hiện action
- Admin-only functions: Check role

### 8.3 Data Validation
- Validate amount > 0
- Validate dates
- Sanitize user input (nội dung CK)

### 8.4 API Rate Limiting
- VietQR API không yêu cầu authentication
- Không có rate limit rõ ràng
- Recommend: Cache QR images sau khi tải

---

## 9. References

- VietQR API: https://vietqr.io/
- VietQR Documentation: https://vietqr.io/docs
- Bank BIN List: https://api.vietqr.io/v2/banks
- QRCoder Library: https://github.com/codebude/QRCoder
- Dapper ORM: https://github.com/DapperLib/Dapper

---

**Generated with Claude Code - Anthropic 2025**
