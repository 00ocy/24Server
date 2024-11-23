using System;
using System.IO;

namespace NetworkLibrary
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        static Logger()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        private static void WriteLog(string logType, string message, string clientId, string ipAddress, int port)
        {
            // 클라이언트 ID 폴더 생성
            string clientDirectory = Path.Combine(LogDirectory, clientId ?? "Unknown");
            if (!Directory.Exists(clientDirectory))
            {
                Directory.CreateDirectory(clientDirectory);
            }

            // 로그 파일 생성
            string fileName = Path.Combine(clientDirectory, $"{logType}_Log_{DateTime.Now:yyyy-MM-dd}.txt");
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [IP: {ipAddress}] [Port: {port}] {message}";

            try
            {
                File.AppendAllText(fileName, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그 기록 중 오류 발생: {ex.Message}");
            }
        }

        public static void LogMessage(string message, ClientInfo clientInfo)
        {
            WriteLog("Message", message, clientInfo?.Id, clientInfo?.IpAddress, clientInfo?.Port ?? 0);
        }

        public static void LogLogin(string clientId, ClientInfo clientInfo)
        {
            WriteLog("Login", "로그인 성공", clientId, clientInfo?.IpAddress, clientInfo?.Port ?? 0);
        }

        public static void LogFileTransfer(string fileName, uint fileSize, ClientInfo clientInfo)
        {
            string message = $"파일 전송: {fileName} ({fileSize} bytes)";
            WriteLog("FileTransfer", message, clientInfo?.Id, clientInfo?.IpAddress, clientInfo?.Port ?? 0);
        }
    }
}
