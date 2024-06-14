using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace udp_03
{
    internal class Program
    {
        public static Socket sock;

        static void Main(string[] args)
        {
            var remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);
            ConnectedUDP(remoteEP);
        }

        private static void ConnectedUDP(IPEndPoint remoteEP)
        {
            // 소켓 생성
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // 바인드 (선택이긴 함)
            sock.Bind(new IPEndPoint(IPAddress.Loopback, 25001));

            // 스레드 시작: 데이터를 지속적으로 수신하는 부분을 별도의 스레드에서 실행
            Thread receiverThread = new Thread(ReceiveData);
            receiverThread.Start();

            // 사용자 입력을 지속적으로 송신
            while (true)
            {
                string userInput = Console.ReadLine();
                if (string.IsNullOrEmpty(userInput))
                {
                    break;
                }
                byte[] buffer = Encoding.UTF8.GetBytes(userInput);
                int retval = sock.SendTo(buffer, buffer.Length, SocketFlags.None, remoteEP);
            }

            // 프로그램이 종료되면 소켓 닫기
            sock.Close();
        }

        private static void ReceiveData()
        {
            // 수신 준비
            var receiver = new IPEndPoint(IPAddress.Any, 0);
            EndPoint recRef = (EndPoint)receiver;

            // 지속적으로 데이터를 받기 위한 루프
            while (true)
            {
                byte[] buffer = new byte[1024];
                Console.WriteLine("Before receiveFrom ~~");
                // 동기식 함수로 데이터 수신이 없으면 Blocking 상태를 유지하고 더이상 코드가 진행되지 않는다
                int recvbyte = sock.ReceiveFrom(buffer, ref recRef);
                Console.WriteLine($"After receiveFrom [{recvbyte}] bytes");
                Console.WriteLine($"[Server] {Encoding.UTF8.GetString(buffer, 0, recvbyte)}");
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
