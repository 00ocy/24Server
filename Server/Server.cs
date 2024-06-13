using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private const int Port = 37000;
    private static readonly IPAddress MulticastAddress = IPAddress.Parse("224.0.0.10");
    private static UdpClient udpClient;
    private static readonly string LogFilePath = "C:\\Logs\\log.txt";  // 파일 저장 경로

    static void Main(string[] args)
    {
        // 로그 파일 디렉토리 생성
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));

        udpClient = new UdpClient();
        udpClient.JoinMulticastGroup(MulticastAddress);

        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        Console.WriteLine($"Server started on port {Port}");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Task.Run(() => HandleClient(client));
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
        Console.WriteLine($"Client connected: {clientEndPoint}");

        await LogMessage(clientEndPoint, "Connected");

        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            while (true)
            {
                try
                {
                    // 메세지 크기를 먼저 읽음
                    char[] sizeBuffer = new char[4];
                    int readSize = await reader.ReadAsync(sizeBuffer, 0, sizeBuffer.Length);
                    if (readSize == 0) break;

                    int messageSize = int.Parse(new string(sizeBuffer));

                    // 메세지 본문 읽기
                    char[] messageBuffer = new char[messageSize];
                    int readMessage = await reader.ReadAsync(messageBuffer, 0, messageBuffer.Length);
                    if (readMessage == 0) break;

                    string message = new string(messageBuffer);
                    await LogMessage(clientEndPoint, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }
        }

        await LogMessage(clientEndPoint, "Disconnected");
        Console.WriteLine($"Client disconnected: {clientEndPoint}");
        client.Close();
    }

    private static async Task LogMessage(IPEndPoint clientEndPoint, string message)
    {
        string logMessage = $@"
        {{
            ""LogTime"": ""{DateTime.Now.ToString("o")}"",
            ""UserEP"": ""{clientEndPoint}"",
            ""MSG"": ""{message}""
        }}";

        byte[] data = Encoding.UTF8.GetBytes(logMessage);
        udpClient.Send(data, data.Length, new IPEndPoint(MulticastAddress, Port));

        await File.AppendAllTextAsync(LogFilePath, logMessage + Environment.NewLine);
    }
}
