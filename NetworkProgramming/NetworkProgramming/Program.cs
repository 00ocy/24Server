using System.Net;
using System.Net.Sockets;
using NetworkLibrary;

namespace NetworkProgramming
{
    internal class Program
    {
        public static void Main()
        {
            IPEndPoint homeServerEp = new IPEndPoint(IPAddress.Parse("192.168.0.38"), 25000);
            IPEndPoint schoolServerEp= new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);

            TcpServer tcpServer = new TcpServer(schoolServerEp);
            tcpServer.Start();
        }

    }
}
