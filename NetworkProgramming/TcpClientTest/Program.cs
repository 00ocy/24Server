
namespace TcpClientTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FtpClient ftpClient = new FtpClient("127.0.0.1", 25000);
            ftpClient.ConnectServer();
        }
    }

}