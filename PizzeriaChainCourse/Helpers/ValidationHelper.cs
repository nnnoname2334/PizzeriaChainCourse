using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PizzeriaChainCourse
{
    public static class ValidationHelper
    {
        // Только буквы, пробел и дефис
        public static bool IsName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, @"^[\p{L}\- ]+$");
        }

        // Приводит телефон к виду +7XXXXXXXXXX, при неверном формате возвращает null
        public static string NormalizePhone(string input)
        {
            string digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.Length == 11 && (digits[0] == '7' || digits[0] == '8'))
                return "+7" + digits.Substring(1);
            if (digits.Length == 10)
                return "+7" + digits;
            return null;
        }

        // Разбирает число с точкой или запятой
        public static bool TryParseAmount(string text, out decimal value)
        {
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            text = text.Trim().Replace(".", sep).Replace(",", sep);
            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out value);
        }
    }
}