using System.Security.Cryptography;
using System.Text;

namespace SecurityLibrary
{
    public static class AESHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes

        public static byte[] Encrypt(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                throw new ArgumentException("암호화할 데이터가 비어 있습니다.", nameof(plainBytes));

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("암호화 중 오류가 발생했습니다.", ex);
            }
        }

        public static byte[] Decrypt(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0)
                throw new ArgumentException("복호화할 데이터가 비어 있습니다.", nameof(encryptedBytes));

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
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
