using System.Security.Cryptography;
using System.Text;

namespace nia_api.Services
{
    public class HashPassword
    {
        public string Hash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }

        public bool VerifyPassword(string enteredPassword, string storedHash)
        {
            string newHash = HashPassword(enteredPassword);
            return newHash == storedHash;
        }
    }
}
