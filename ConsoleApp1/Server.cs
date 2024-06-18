using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lean
{
    internal class Server
    {
        private static Socket sock;
        private static IPEndPoint serverEP;

        static void Main(string[] args)
        {
            serverEP = new IPEndPoint(IPAddress.Any, 25000);
            ServerStart(serverEP);
        }

        private static void ServerStart(IPEndPoint serverEP)
        {
            // 생성
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // 바인드
            sock.Bind(serverEP);
            // 스레드로 클라이언트 받기
            Thread recThread = new Thread(RecStart);
            recThread.Start();
        }

        private static void RecStart()
        {
            // 수신 준비 클라이언트 EP
            var recipEP = new IPEndPoint(IPAddress.Any, 0);

            // 수신 준비 버퍼
            byte[] buffer = new byte[1024];

            while (true)
            {
                // 수신 준비 리시브프롬
                EndPoint clientEP = (EndPoint)recipEP;

                // 수신
                int retval = sock.ReceiveFrom(buffer, ref clientEP);
                string retstring = Encoding.UTF8.GetString(buffer,0,retval);

                // 수신 확인
                Console.WriteLine($"[recdata] retval: {retval} / retstring: {retstring}");

                // 에코
                sock.SendTo(buffer, buffer.Length, SocketFlags.None, clientEP);
            }
        }
    }
}
