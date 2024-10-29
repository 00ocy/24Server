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
            IPEndPoint schoolServerEp= new IPEndPoint(IPAddress.Parse("172.18.27.201"), 25000);

            TcpServer tcpServer = new TcpServer(schoolServerEp);
            tcpServer.Start();
        }

    }
}
