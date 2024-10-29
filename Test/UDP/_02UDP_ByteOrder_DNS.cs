using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_udp_02
{
    internal class Program
    {
        private static Socket remoteSocket;
        private static byte[] buffer2;

        static void Main(string[] args)
        {
            // URL 사용하기
            string tempUrl = "www.naver.com";
            // DNS 패킷을 참조하여 IP 주소를 확인
            var TempEntry = Dns.GetHostEntry(tempUrl);
            // hostEntry중 AddressList 배열에서 주소 확인
            var remoteAddress = IPAddress.Parse(TempEntry.AddressList[0].ToString());
            // 원격 주소지용 EndPoint 객체 생성
            var remoteEP = new IPEndPoint(remoteAddress, 25000);

            // 함수 실행 
            RunUDP_ByteOrderAndUsingDNS(remoteEP);
        }

        private static void RunUDP_ByteOrderAndUsingDNS(IPEndPoint remoteEP)
        {
            // 각 예외상황 socketException, NullReferenceException 확인용
            try
            {
                remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // 이미 사용중인 포트를 사용하려고 할때 예외 발생
                // 다른 툴에서 25000 사용하여 테스트 함
                //remoteSocket.Bind(new IPEndPoint(IPAddress.Any, 25000)); 
                remoteSocket.Bind(new IPEndPoint(IPAddress.Any, 25001));

                short x1 = 0x1234;
                int y1 = 0x12345678;
                short x2 = IPAddress.HostToNetworkOrder(x1);
                int y2 = IPAddress.HostToNetworkOrder(y1);
                Console.WriteLine("0x{0:x} -> 0x{1:x}", x1, x2);
                Console.WriteLine("0x{0:x} -> 0x{1:x}", y1, y2);
                byte[] sample_X1 = BitConverter.GetBytes(x1);
                byte[] sample_X2 = BitConverter.GetBytes(x2);
                byte[] sample_Y1 = BitConverter.GetBytes(y1);
                byte[] sample_Y2 = BitConverter.GetBytes(y2);
                byte[] sample_str = Encoding.UTF8.GetBytes("abcd");
                // 직접 저장해도되고 선언과 동시에 지정해도 됨.
                byte[] test = new byte[5] { 0x12,0x34,0x56,0x78,0x00};                
                //test[0] = 0x12;
                //test[1] = 0x34;
                //test[2] = 0x56;
                //test[3] = 0x78;

                // Byte Order는 기본 데이터 타입으로 변결될때 적용된다.
                // 따라서 수신하여 Int 또는 short에 저장할때 변경됨
                // 변경되는 흐름을 정확히 볼것.
                remoteSocket.SendTo(sample_X1, sample_X1.Length, SocketFlags.None, remoteEP);
                remoteSocket.SendTo(sample_X2, sample_X2.Length, SocketFlags.None, remoteEP);
                remoteSocket.SendTo(sample_Y1, sample_Y1.Length, SocketFlags.None, remoteEP);
                remoteSocket.SendTo(sample_Y2, sample_Y2.Length, SocketFlags.None, remoteEP);

                // 문자열은 기본적으로 바이트배열이기때문에 그대로 네트워크 바이트 배열로 전송
                remoteSocket.SendTo(sample_str, sample_str.Length, SocketFlags.None, remoteEP);
                // 바이트 순서를 확인하기 위한 임시 바이트 배열
                remoteSocket.SendTo(test, test.Length, SocketFlags.None, remoteEP);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            remoteSocket.Close();
            Console.WriteLine("Closed from localhost~!");
            return;
        }
    }
}
