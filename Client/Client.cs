using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lean
{
    internal class Client
    {
        private static Socket sock;
        private static IPEndPoint serverEP;

        static void Main(string[] args)
        {
            serverEP = new IPEndPoint(IPAddress.Loopback, 25000);
            RunUdp(serverEP);
        }

        private static void RunUdp(IPEndPoint serverEP)
        {
            // 소켓 생성
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // 바인드 (옵션)
            sock.Bind(new IPEndPoint(IPAddress.Loopback, 25001));
            // 스레드 나누기
            Thread RecThread = new Thread(RecFun);
            RecThread.Start();
            // 송신 스레드 버퍼 준비
            // 송신 스레드 송신
            while (true)
            {
                string Msg = Console.ReadLine();
                byte[] buffer = Encoding.UTF8.GetBytes(Msg);
                int retval = sock.SendTo(buffer, buffer.Length, SocketFlags.None, serverEP);
                Console.WriteLine($" Send Msg: {Msg}");
            }
            // 수신 스레드 리시브 준비 (함수로 뺄 수도 있음)
            // 수신 스레드 리시브
        }

        private static void RecFun()
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Recref = (EndPoint)clientEP;
            while (true)
            {
                byte[] buffer2 = new byte[4000];
                int recval = sock.ReceiveFrom(buffer2, SocketFlags.None, ref Recref);
                string Msg2 = Encoding.UTF8.GetString(buffer2);
                Console.WriteLine(Msg2);
                if (recval == 0)
                {
                    Console.WriteLine("server closed");
                    return;
                }
            }
        }
    }
}