using System.Text.RegularExpressions;

namespace QuanLyNhaTro.BLL.Helpers
{
    /// <summary>
    /// Helper class cho validation
    /// </summary>
    public static partial class ValidationHelper
    {
        /// <summary>
        /// Validate email
        /// </summary>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email không bắt buộc

            return EmailRegex().IsMatch(email);
        }

        /// <summary>
        /// Validate số điện thoại Việt Nam
        /// </summary>
        public static bool IsValidPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // Phone không bắt buộc

            return PhoneRegex().IsMatch(phone);
        }

        /// <summary>
        /// Validate CCCD (12 số)
        /// </summary>
        public static bool IsValidCCCD(string? cccd)
        {
            if (string.IsNullOrWhiteSpace(cccd))
                return false;

            return CCCDRegex().IsMatch(cccd);
        }

        /// <summary>
        /// Validate không để trống
        /// </summary>
        public static bool IsNotEmpty(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Validate số dương
        /// </summary>
        public static bool IsPositive(decimal value)
        {
            return value > 0;
        }

        /// <summary>
        /// Validate số không âm
        /// </summary>
        public static bool IsNonNegative(decimal value)
        {
            return value >= 0;
        }

        /// <summary>
        /// Validate ngày trong tương lai
        /// </summary>
        public static bool IsFutureDate(DateTime date)
        {
            return date.Date > DateTime.Today;
        }

        /// <summary>
        /// Validate ngày kết thúc sau ngày bắt đầu
        /// </summary>
        public static bool IsEndDateAfterStartDate(DateTime startDate, DateTime endDate)
        {
            return endDate > startDate;
        }

        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"^(0|84|\+84)?[3|5|7|8|9][0-9]{8}$")]
        private static partial Regex PhoneRegex();

        [GeneratedRegex(@"^[0-9]{12}$")]
        private static partial Regex CCCDRegex();
    }
}
