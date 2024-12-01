using System.Security.Cryptography;

namespace SecurityLibrary
{
    public static class Hashing
    {

        // 파일의 해시 계산
        public static string CalculateFileHash(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

    }
}
