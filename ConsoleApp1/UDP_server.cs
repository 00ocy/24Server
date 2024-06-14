using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace z_Lean
{
    internal class UDP_server
    {
        private static Socket sock;
        private static IPEndPoint serverEP;
        private static EndPoint clientEP;

        static void Main(string[] args)
        {
            serverEP = new IPEndPoint(IPAddress.Any, 25000);
            RecStart(serverEP);
        }

        private static void RecStart(IPEndPoint serverEP)
        {
            // 소켓 생성
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // 바인드
            sock.Bind(serverEP);

            // 수신 후 송신하는 스레드 생성
            Thread receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }

        private static void ReceiveData()
        {
            byte[] buffer = new byte[1500];
            var receiveEP = new IPEndPoint(IPAddress.Any, 0);
            clientEP = (EndPoint)receiveEP;

            while (true)
            {
                int retval = sock.ReceiveFrom(buffer, ref clientEP);
                string receivedMsg = Encoding.UTF8.GetString(buffer, 0, retval);
                Console.WriteLine($"Received: {receivedMsg}");

                // Echo the received message back to the client
                SendData(receivedMsg);
            }
        }

        private static void SendData(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            sock.SendTo(buffer, clientEP);
        }
    }
}
