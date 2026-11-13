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
    }
}