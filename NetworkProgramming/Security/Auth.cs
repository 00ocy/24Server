namespace SecurityLibrary
{
    public static class Auth
    {
        public static bool Login()
        {
            Console.WriteLine("아이디를 입력하세요:");
            string userId = Console.ReadLine();
            string encryptedId = AESHelper.Encrypt(userId);

            string userPath = Path.Combine("User", encryptedId);

            if (!Directory.Exists(userPath))
            {
                Console.WriteLine("존재하지 않는 ID입니다.");
                return false;
            }

            Console.WriteLine("비밀번호를 입력하세요:");
            string password = Console.ReadLine();
            string encryptedPassword = AESHelper.Encrypt(password);

            string passwordFilePath = Path.Combine(userPath, "password.txt");
            if (File.Exists(passwordFilePath))
            {
                string storedPassword = File.ReadAllText(passwordFilePath);
                if (storedPassword == encryptedPassword)
                {
                    Console.WriteLine("로그인 성공!");
                    return true;
                }
            }

            Console.WriteLine("비밀번호가 틀렸습니다.");
            return false;
        }
        public static void Register()
        {
            Console.WriteLine("아이디를 입력하세요:");
            string userId = Console.ReadLine();
            string encryptedId = AESHelper.Encrypt(userId);

            string userPath = Path.Combine("User", encryptedId);

            if (Directory.Exists(userPath))
            {
                Console.WriteLine("사용중인 ID입니다.");
                return;
            }

            Directory.CreateDirectory(userPath);

            Console.WriteLine("비밀번호를 입력하세요:");
            string password = Console.ReadLine();
            string encryptedPassword = AESHelper.Encrypt(password);

            string passwordFilePath = Path.Combine(userPath, "password.txt");
            File.WriteAllText(passwordFilePath, encryptedPassword);

            Console.WriteLine("회원가입 완료!");
        }
    }
}
