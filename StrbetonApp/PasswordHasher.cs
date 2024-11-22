using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StrbetonApp
{
    public static class PasswordHasher
    {
        private static string Salt = "nelli"; 

        public static string HashPassword(string password)
        {
            using (var md5 = MD5.Create())
            {
               
                string saltedPassword = Salt + password;
                byte[] inputBytes = Encoding.UTF8.GetBytes(saltedPassword);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); 
            }
        }
        public static bool VerifyPassword(string storedHash, string enteredPassword)
        {
            string enteredHash = HashPassword(enteredPassword);
            return storedHash == enteredHash; 
        }
    }
}
