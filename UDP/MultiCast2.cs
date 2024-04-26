using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_udp_03_1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // multicast address
            string MULTICASTIP = "224.0.0.10";
            int LOCALPORT = 9000;

            var remoteEP = new IPEndPoint(IPAddress.Parse(MULTICASTIP), LOCALPORT);
            recvMulticast(remoteEP);
        }

        private static void recvMulticast(IPEndPoint remoteEP)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //recv setsocketoption
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            sock.Bind(new IPEndPoint(IPAddress.Any, remoteEP.Port));

            /// 수업시간이 모자라 설명이 안된 부분
            // 멀티캐스트 그룹 가입
            var mcastOption = new MulticastOption(IPAddress.Parse(remoteEP.Address.ToString()), IPAddress.Any);
            // 멀티캐스용 소켓 옵션 변경
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
            ///

            byte[] buffer = new byte[1500];
            // 수신된 패킷의 발송자 정보를 얻기 위한 데이터객체 생성
            // IPEndPoint 에 생성자는 반드시 주소와 포트를 적어야하기에 의미없는 내용으로 채움
            var receiver = new IPEndPoint(IPAddress.Any, 0);
            // ReceiveFrom 함수의 2번째 인자가 EndPoint이며 ref 타입이여서 타입캐스팅후에 전달
            EndPoint recvEP = (EndPoint)receiver;

            while (true)
            {
                // 로컬호스트로 테스트하면 발송과 동시에 수신되어 자기 IP번호와 발송 포트번호가 보임
                int recvBytes = sock.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref recvEP);
                Console.WriteLine($"recvByte from [{((IPEndPoint)recvEP).Address}:{((IPEndPoint)recvEP).Port}] ::" +
                                    $"[{recvBytes}]-{Encoding.UTF8.GetString(buffer)}");
                Thread.Sleep(500);

            }

            /// 수업시간이 모자라 설명이 안된 부분
            // 멀티캐스트 그룹 탈퇴
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, mcastOption);
            sock.Close();
        }
    }
}
