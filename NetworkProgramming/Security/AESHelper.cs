using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecurityLibrary
{
    public static class AESHelper
    {
        // 정확히 32 문자 키와 16 문자 IV 사용
        private static readonly string Key = "12345678901234567890123456789012"; // 32 문자 키
        private static readonly string IV = "1234567890123456"; // 16 문자 IV

        public static string Encrypt(string plainText)
        {
            // 입력값이 null이거나 비어있는지 확인
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("암호화할 텍스트가 비어 있습니다.", nameof(plainText));

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(Key);
                    aes.IV = Encoding.UTF8.GetBytes(IV);

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("암호화 중 오류가 발생했습니다.", ex);
            }
        }

        public static string Decrypt(string encryptedText)
        {
            // 입력값이 null이거나 비어있는지 확인
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("복호화할 텍스트가 비어 있습니다.", nameof(encryptedText));

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(Key);
                    aes.IV = Encoding.UTF8.GetBytes(IV);

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                        byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("복호화 중 오류가 발생했습니다.", ex);
            }
        }
    }
}
