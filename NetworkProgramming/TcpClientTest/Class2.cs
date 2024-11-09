/*using System;
using System.Security.Cryptography;
using System.Text;

class RSACSPSample
{
    static void Main()
    {
        try
        {
            // UnicodeEncoder를 생성하여 바이트 배열과 문자열 간의 변환을 수행
            UnicodeEncoding ByteConverter = new UnicodeEncoding();

            // 원본 데이터, 암호화된 데이터, 복호화된 데이터를 저장할 바이트 배열 생성
            byte[] dataToEncrypt = ByteConverter.GetBytes("Data to Encrypt");
            byte[] encryptedData;
            byte[] decryptedData;

            // RSACryptoServiceProvider의 새 인스턴스를 생성하여 공개 키와 비공개 키 데이터를 생성
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                // 데이터를 암호화합니다. 공개 키 정보(RSA.ExportParameters(false))와 OAEP 패딩 미사용(false)을 전달
                encryptedData = RSAEncrypt(dataToEncrypt, RSA.ExportParameters(false), false);

                // 암호화된 데이터를 Base64 문자열로 변환하여 콘솔에 출력
                string encryptedBase64 = Convert.ToBase64String(encryptedData);
                Console.WriteLine("Encrypted data (Base64): {0}", encryptedBase64);

                // 데이터를 복호화합니다. 비공개 키 정보(RSA.ExportParameters(true))와 OAEP 패딩 미사용(false)을 전달
                decryptedData = RSADecrypt(encryptedData, RSA.ExportParameters(true), false);

                // 복호화된 평문을 문자열로 변환하여 콘솔에 출력
                Console.WriteLine("Decrypted plaintext: {0}", ByteConverter.GetString(decryptedData));
            }
        }
        catch (ArgumentNullException)
        {
            // 암호화가 실패했을 경우 예외를 캐치하고 메시지를 출력
            Console.WriteLine("Encryption failed.");
        }
    }

    // RSA 암호화 메서드
    public static byte[] RSAEncrypt(byte[] DataToEncrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
    {
        try
        {
            byte[] encryptedData;
            // RSACryptoServiceProvider의 새 인스턴스를 생성
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                // RSA 키 정보를 가져와서 공개 키 정보만 포함하도록 설정
                RSA.ImportParameters(RSAKeyInfo);

                // 전달된 바이트 배열을 암호화하고 OAEP 패딩 사용 여부를 지정
                encryptedData = RSA.Encrypt(DataToEncrypt, DoOAEPPadding);
            }
            return encryptedData;
        }
        catch (CryptographicException e)
        {
            // 암호화 예외가 발생한 경우 예외 메시지를 출력
            Console.WriteLine(e.Message);
            return null;
        }
    }

    // RSA 복호화 메서드
    public static byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
    {
        try
        {
            byte[] decryptedData;
            // RSACryptoServiceProvider의 새 인스턴스를 생성
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                // RSA 키 정보를 가져와서 비공개 키 정보가 포함되도록 설정
                RSA.ImportParameters(RSAKeyInfo);

                // 전달된 바이트 배열을 복호화하고 OAEP 패딩 사용 여부를 지정
                decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
            }
            return decryptedData;
        }
        catch (CryptographicException e)
        {
            // 복호화 예외가 발생한 경우 예외 메시지를 출력
            Console.WriteLine(e.ToString());
            return null;
        }
    }
}

*/