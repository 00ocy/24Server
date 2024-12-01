using System.Net;
using NetworkLibrary;

namespace NetworkProgramming
{
    internal class Program
    {
        public static void Main()
        {
            IPEndPoint ServerEp= new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);

            TcpServer tcpServer = new TcpServer(ServerEp);
            tcpServer.Start();
        }

    }
}
