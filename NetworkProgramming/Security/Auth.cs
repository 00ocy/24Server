using System.Text;

namespace SecurityLibrary
{
    public static class Auth
    {

        public static bool LoginCheck(byte[]? body)
        {
            if (body == null || body.Length == 0)
            {
                Console.WriteLine("받은 데이터가 없습니다.");
                return false;
            }

            // 바디를 문자열로 변환 후 ':'로 분리
            string bodyString = Encoding.UTF8.GetString(body);
            string[] parts = bodyString.Split(':');
            if (parts.Length != 2)
            {
                Console.WriteLine("받은 데이터 형식이 잘못되었습니다.");
                return false;
            }

            string ID = parts[0]; // 암호화된 ID
            string encryptedPw = parts[1]; // 암호화된 PW

            try
            {
                // 사용자 폴더 경로 (암호화된 ID를 그대로 사용)
                string userFolderPath = Path.Combine("User", ID);
                if (!Directory.Exists(userFolderPath))
                {
                    Console.WriteLine("사용자 폴더가 존재하지 않습니다.");
                    return false;
                }

                // 사용자 폴더 내의 pw 파일 경로
                string passwordFilePath = Path.Combine(userFolderPath, "pw");
                if (!File.Exists(passwordFilePath))
                {
                    Console.WriteLine("비밀번호 파일이 존재하지 않습니다.");
                    return false;
                }

                // 파일에서 저장된 암호화된 비밀번호 읽기
                string storedEncryptedPw = File.ReadAllText(passwordFilePath).Trim();

                // 비밀번호 비교
                if (storedEncryptedPw == encryptedPw)
                {
                    Console.WriteLine("로그인 성공");
                    return true;
                }
                else
                {
                    Console.WriteLine("비밀번호가 일치하지 않습니다.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그인 확인 중 오류 발생: {ex.Message}");
                return false;
            }
        }

     

        public static bool DuplicateCheck(byte[]? body)
        {
            if (body == null || body.Length == 0)
            {
                Console.WriteLine("받은 데이터가 없습니다.");
                return false;
            }

            // 바디를 문자열로 변환
            string ID = Encoding.UTF8.GetString(body);

            // 사용자 폴더 경로 (암호화된 ID를 그대로 사용)
            string userFolderPath = Path.Combine("User", ID);

            if (!Directory.Exists(userFolderPath))
            {
                Console.WriteLine("중복되지 않은 ID.");
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool Register(byte[]? body)
        {
            if (body == null || body.Length == 0)
            {
                Console.WriteLine("받은 데이터가 없습니다.");
                return false;
            }

            // 바디를 문자열로 변환 후 ':'로 분리
            string bodyString = Encoding.UTF8.GetString(body);
            string[] parts = bodyString.Split(':');
            if (parts.Length != 2)
            {
                Console.WriteLine("받은 데이터 형식이 잘못되었습니다.");
                return false;
            }

            string ID = parts[0]; // ID
            string encryptedPw = parts[1]; // 암호화된 PW

            try
            {
                // User 폴더 경로
                string userFolderPath = Path.Combine("User", ID);

                // 중복 확인
                if (Directory.Exists(userFolderPath))
                {
                    Console.WriteLine("이미 존재하는 ID입니다.");
                    return false;
                }

                // 사용자 폴더 생성
                Directory.CreateDirectory(userFolderPath);

                // 암호화된 비밀번호를 "pw" 파일에 저장
                string passwordFilePath = Path.Combine(userFolderPath, "pw");
                File.WriteAllText(passwordFilePath, encryptedPw);

                Console.WriteLine("회원가입이 완료되었습니다.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"회원가입 중 오류 발생: {ex.Message}");
                return false;
            }
        }

    }
}
