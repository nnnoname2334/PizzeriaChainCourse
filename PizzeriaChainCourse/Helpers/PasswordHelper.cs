using System.Security.Cryptography;
using System.Text;

namespace PizzeriaChainCourse
{
    public static class PasswordHelper
    {
        // Так же, как хешировались пароли в БД: SHA-256 от строки в UTF-16.
        // Тот же алгоритм использует окно входа
        public static byte[] Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.Unicode.GetBytes(password));
            }
        }
    }
}