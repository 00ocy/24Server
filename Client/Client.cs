using System.Net.Sockets;
using System.Net;
using System.Text;

namespace console_udp_03
{
    internal class Program
    {
        public static Socket sock;
        static void Main(string[] args)
        {
            var remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"),25000);
            ConnectedUDP(remoteEP);
        
        }

        private static void ConnectedUDP(IPEndPoint remoteEP)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(new IPEndPoint(IPAddress.Loopback,25001));
            // 데이터 준비
            string Msg = "connecting";
            byte[] buffer = Encoding.UTF8.GetBytes(Msg);
            int size = buffer.Length;

            // 송신
            int retval = sock.SendTo(buffer,buffer.Length,SocketFlags.None, remoteEP);

            //// 수신 준비 ////
            // 수신된 패킷의 발송자 정보를 얻기 위한 데이터객체 생성
            // IPEndPoint 에 생성자는 반드시 주소와 포트를 적어야하기에 의미없는 내용으로 채움
            var receiver = new IPEndPoint(IPAddress.Any, 0);
            // ReceiveFrom 함수의 2번째 인자가 EndPoint이며 ref 타입이여서 타입캐스팅후에 전달
            EndPoint remoteForReceive = (EndPoint)receiver;

            // 지속적으로 데이터를 받기 위한 루프
            while (true)
            {
                byte[] buffer2 = new byte[1024];
                Console.WriteLine("Before recevFrom ~~");
                // 동기식 함수로 데이터 수신이 없으면 Blocking 상태를 유지하고 더이상 코드가 진행되지 않는다
                int recvbyte = sock.ReceiveFrom(buffer2, ref remoteForReceive);
                Console.WriteLine($"after recevFrom [{recvbyte}]bytes");
                Console.WriteLine($"[Server] {Encoding.UTF8.GetString(buffer2)}");
                // UDP는 연결이 끝났는지 모른다.
                // 데이터가 없는 패킷을 보냈을때만 반응
                if (recvbyte == 0)
                {
                    Console.WriteLine("Closed from server~!");
                    return;
                }
            }

        }
    }
}
