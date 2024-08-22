using System;
using System.Security.Cryptography;
using System.Text;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Security
{
    public class Encryption
    {
        private static readonly string EncryptionKey = "1234567890123456"; // this needs to be saved in a safer location once we go public
        
        public static (string encryptedText, string iv) Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.GenerateIV();

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var ms = new System.IO.MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new System.IO.StreamWriter(cs))
            {
                writer.Write(plainText);
            }
                    
            // return the encrypted text and the IV

            return (Convert.ToBase64String(ms.ToArray()), Convert.ToBase64String(aes.IV));
        }

        public static string Decrypt(string encryptedText, string iv)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = Convert.FromBase64String(iv);

            ICryptoTransform decrypt = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new System.IO.MemoryStream(Convert.FromBase64String(encryptedText));
            using var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Read);
            using var reader = new System.IO.StreamReader(cs);

            try
            {
                return reader.ReadToEnd();
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
        
    }
}
