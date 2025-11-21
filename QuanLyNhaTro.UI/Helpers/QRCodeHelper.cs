using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Text.Json;

namespace QuanLyNhaTro.UI.Helpers
{
    /// <summary>
    /// Helper class để tạo mã QR thanh toán VietQR
    /// Sử dụng API từ img.vietqr.io (Quick Link)
    /// </summary>
    public static class QRCodeHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // Danh sách mã BIN ngân hàng phổ biến
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

        /// <summary>
        /// Tạo URL Quick Link VietQR
        /// Format: https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-{TEMPLATE}.png?amount={AMOUNT}&addInfo={DESCRIPTION}&accountName={ACCOUNT_NAME}
        /// </summary>
        public static string GetVietQRImageUrl(string bankBin, string accountNumber, decimal amount, string description, string accountName, string template = "compact2")
        {
            var encodedDesc = Uri.EscapeDataString(description);
            var encodedName = Uri.EscapeDataString(accountName);

            var url = $"https://img.vietqr.io/image/{bankBin}-{accountNumber}-{template}.png";

            var queryParams = new List<string>();
            if (amount > 0)
                queryParams.Add($"amount={amount:0}");
            if (!string.IsNullOrEmpty(description))
                queryParams.Add($"addInfo={encodedDesc}");
            if (!string.IsNullOrEmpty(accountName))
                queryParams.Add($"accountName={encodedName}");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            return url;
        }

        /// <summary>
        /// Lấy mã BIN từ mã ngân hàng
        /// </summary>
        public static string GetBankBin(string bankCode)
        {
            return BankBins.TryGetValue(bankCode.ToUpper(), out var bin) ? bin : bankCode;
        }

        /// <summary>
        /// Tải ảnh QR từ VietQR API
        /// </summary>
        public static async Task<Bitmap?> GetVietQRImageAsync(string bankBin, string accountNumber, decimal amount, string description, string accountName, string template = "compact2")
        {
            try
            {
                var url = GetVietQRImageUrl(bankBin, accountNumber, amount, description, accountName, template);
                var imageBytes = await _httpClient.GetByteArrayAsync(url);

                using var ms = new MemoryStream(imageBytes);
                return new Bitmap(ms);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching VietQR image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tải ảnh QR từ VietQR API với bankCode (VCB, MB, TCB...)
        /// </summary>
        public static async Task<Bitmap?> GetVietQRImageByBankCodeAsync(string bankCode, string accountNumber, decimal amount, string description, string accountName, string template = "compact2")
        {
            var bankBin = GetBankBin(bankCode);
            return await GetVietQRImageAsync(bankBin, accountNumber, amount, description, accountName, template);
        }

        /// <summary>
        /// Tạo mã QR từ nội dung text (fallback khi không có internet)
        /// </summary>
        public static Bitmap GenerateQRCode(string content, int pixelsPerModule = 10)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.QRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule);
        }

        /// <summary>
        /// Tạo mã QR với màu tùy chỉnh
        /// </summary>
        public static Bitmap GenerateQRCode(string content, Color darkColor, Color lightColor, int pixelsPerModule = 10)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.QRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule, darkColor, lightColor, true);
        }

        /// <summary>
        /// Tạo mã QR thanh toán ngân hàng - sử dụng VietQR API
        /// Fallback về QRCoder nếu không có internet
        /// </summary>
        public static async Task<Bitmap> GenerateBankQRAsync(string bankCode, string accountNumber, string accountName, decimal amount, string description)
        {
            // Thử lấy từ VietQR API trước
            var qrImage = await GetVietQRImageByBankCodeAsync(bankCode, accountNumber, amount, description, accountName);

            if (qrImage != null)
                return qrImage;

            // Fallback: Tạo QR code offline với nội dung text
            var content = $"Ngân hàng: {bankCode}\n" +
                          $"STK: {accountNumber}\n" +
                          $"Chủ TK: {accountName}\n" +
                          $"Số tiền: {amount:N0} VND\n" +
                          $"Nội dung: {description}";
            return GenerateQRCode(content, 6);
        }

        /// <summary>
        /// Tạo mã QR thanh toán - Sync version (hiển thị loading hoặc placeholder)
        /// </summary>
        public static Bitmap GenerateBankQRSync(string bankCode, string accountNumber, string accountName, decimal amount, string description)
        {
            try
            {
                return GenerateBankQRAsync(bankCode, accountNumber, accountName, amount, description).GetAwaiter().GetResult();
            }
            catch
            {
                // Fallback
                var content = $"Ngân hàng: {bankCode}\nSTK: {accountNumber}\nSố tiền: {amount:N0}\nND: {description}";
                return GenerateQRCode(content, 6);
            }
        }

        /// <summary>
        /// Lưu mã QR ra file
        /// </summary>
        public static void SaveQRCode(Bitmap qrBitmap, string filePath, ImageFormat? format = null)
        {
            format ??= ImageFormat.Png;
            qrBitmap.Save(filePath, format);
        }

        /// <summary>
        /// Tạo mã QR và trả về dưới dạng Base64 string
        /// </summary>
        public static string GenerateQRCodeBase64(string content, int pixelsPerModule = 10)
        {
            using var bitmap = GenerateQRCode(content, pixelsPerModule);
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Tạo thông tin thanh toán cho QR
        /// </summary>
        public static PaymentQRInfo CreatePaymentInfo(
            string bankName, string bankCode, string accountNumber, string accountName,
            decimal amount, string transferContent, int maYeuCau, string maPhong)
        {
            var bankBin = GetBankBin(bankCode);
            var qrUrl = GetVietQRImageUrl(bankBin, accountNumber, amount, transferContent, accountName);

            return new PaymentQRInfo
            {
                BankName = bankName,
                BankCode = bankCode,
                BankBin = bankBin,
                AccountNumber = accountNumber,
                AccountName = accountName,
                Amount = amount,
                TransferContent = transferContent,
                MaYeuCau = maYeuCau,
                MaPhong = maPhong,
                QRImageUrl = qrUrl
            };
        }
    }

    /// <summary>
    /// Thông tin thanh toán QR
    /// </summary>
    public class PaymentQRInfo
    {
        public string BankName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string BankBin { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransferContent { get; set; } = string.Empty;
        public int MaYeuCau { get; set; }
        public string MaPhong { get; set; } = string.Empty;
        public string QRImageUrl { get; set; } = string.Empty;

        public string GetDisplayInfo()
        {
            return $"Ngân hàng: {BankName}\n" +
                   $"Số tài khoản: {AccountNumber}\n" +
                   $"Chủ tài khoản: {AccountName}\n" +
                   $"Số tiền: {Amount:N0} VND\n" +
                   $"Nội dung CK: {TransferContent}";
        }

        /// <summary>
        /// Lấy ảnh QR từ VietQR API
        /// </summary>
        public async Task<Bitmap?> GetQRImageAsync()
        {
            return await QRCodeHelper.GetVietQRImageAsync(BankBin, AccountNumber, Amount, TransferContent, AccountName);
        }
    }
}
