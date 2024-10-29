using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_udp_01
{
    internal class Program
    {

        static void Main(string[] args)
        {
            // 소켓 클래스를 사용한  UDP 통신 loopback 또는 다른 컴퓨터
            // loopback 테스트용 주소설정 방법 두가지
            var remoteEP = new IPEndPoint(IPAddress.Loopback, 25000);
            //var remoteforsend = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);
            //// 다른 컴퓨터 테스트용 (자기 컴퓨터 IP확인 하여 적절한 IP 사용하기)
            //var remoteforsend = new IPEndPoint(IPAddress.Parse("xxx.xxx.xxx.xxx"), 25000);
            RunUdp_TEST(remoteEP);
        }

       
        private static void RunUdp_TEST(IPEndPoint remoteEP)
        {
            try
            {
                Socket remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                
                // 클라이언트 모드에서 옵션이므로 주석처리하면 동적 할당된 포트를 사용함
                // 바인딩하면 해당 포트 번호를 사용
                // 서버처럼 해당 포트를 사용하고 데이터 수신을 대기와 같은 상태
                remoteSocket.Bind(new IPEndPoint(IPAddress.Any, 25001));

                                
                string data = "You must study network ~!!!!!!";
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                // NullReferenceException을 위해서 버퍼를 null 변경하고 실행
                //byte[] buffer = null;
                remoteSocket.SendTo(buffer, buffer.Length, SocketFlags.None, remoteEP);

                // var remote1 = new IPEndPoint(IPAddress.Parse("   172.18.27.255"), 25000);
                // remoteSocket.SendTo(buffer, buffer.Length, SocketFlags.None, remote1);

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
                    int recvbyte = remoteSocket.ReceiveFrom(buffer2, ref remoteForReceive);
                    Console.WriteLine($"after recevFrom [{recvbyte}]bytes");

                    // UDP는 연결이 끝났는지 모른다.
                    // 데이터가 없는 패킷을 보냈을때만 반응
                    if (recvbyte == 0)
                    {
                        Console.WriteLine("Closed from server~!");
                        return;
                    }
                }
               
            }
            catch (NullReferenceException ex)
            {
                //발송 데이터가 Null 일때
                Console.WriteLine(ex.ToString());
            }
            catch (SocketException ex)
            {
                // 소켓 설정 에러 or 바인딩 실패
                Console.WriteLine(ex.ToString());   
            }

            Console.WriteLine("Closed from localhost~!");
            return;
        }
    }
}

