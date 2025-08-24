using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ModBusDevExpress.Utils
{
    public static class EncryptionHelper
    {
        private static readonly string EncryptionKey = "ModBusApp2024Key!@#$%";

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                using (Aes aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(EncryptionKey,
                        Encoding.UTF8.GetBytes("SaltForModBus"), 10000);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(
                            plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch
            {
                return plainText;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                using (Aes aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(EncryptionKey,
                        Encoding.UTF8.GetBytes("SaltForModBus"), 10000);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(
                            encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch
            {
                return encryptedText;
            }
        }
    }
}