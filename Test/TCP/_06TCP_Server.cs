using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcp_01svr
{
    internal class Program
    {
        private static Socket sock;

        static void Main(string[] args)
        {
            // 서버 모드에서 EndPoint 설정 방법과 차이점 확인
            // 1. 지정한 IP로 설정한 NIC 또는 네트워크로 수신되는 데이터
            var localEP = new IPEndPoint(IPAddress.Parse("172.18.27.70"), 25000);
            // 2. 지정한 루프백 주소로 수신되는 데이터, 외부 데이터는 수신되진 않음.
            var localEPLoopBack = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);
            localEPLoopBack = new IPEndPoint(IPAddress.Loopback, 25000);
            // 3. 호스트가 소유한 모든 NIC에서 수신되는 데이터
            var localEPALL = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 25000);
            localEPALL = new IPEndPoint(IPAddress.Any, 30000);

            // 로컬 EndPoint로 설정된 서버 시작 
            StartServer(localEPALL);
        }

        private static void StartServer(IPEndPoint localEPALL)
        {
            try
            {
                // 소켓 생성
                sock = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream, ProtocolType.Tcp);

                // 주의. EndPoint 는 추상클래스
                // EndPoint로 운영체제에 리소스 사용 요청
                // 타 프로세스에서 사용 중일때 예외 발생
                Console.WriteLine("[info] -- Server Binding");
                sock.Bind(localEPALL);
                Console.WriteLine($"[info] -- Server Bind to [{localEPALL.Address}]:[{localEPALL.Port}]");
                Console.WriteLine($"[info] -- Server Bind to [{sock.LocalEndPoint}]");

                // 서버 시작. 접속 신호 수신 및 대기열 생성
                Console.WriteLine("[info] -- Server Listenning with limit 15");
                sock.Listen(15);
                Console.WriteLine("[info] -- Start Server ~!");

                // 동기식 함수로 운영체제로 권한이 넘어가 대기상태로 전환
                // 접속자가 있을때 함수가 소켓 정보를 반환
                // 반환된 소켓은 접속자와 통신할 수 있는 소켓이며,
                // 서버가 Listen 을 위해 사용하는 소켓과 구별 됨.
                Console.WriteLine("[info] -- Server blocking in Accept()");
                var clientSock = sock.Accept();
                Console.WriteLine("[info] -- Client connected");
                IPEndPoint clientEP = (IPEndPoint)clientSock.RemoteEndPoint;
                Console.WriteLine($"[Client] -- from [{clientEP.Address}]:[{clientEP.Port}]");

                // 데이터 송수신 (반환된 클라이언트 소켓 사용)
                Console.WriteLine("[data] ==> transfer data from client to server");
                // 데이터 수신
                byte[] buffer = new byte[1500];
                int retval = clientSock.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                Console.WriteLine($"[info] -- recvBytes[{retval}]");

                // 주의 버퍼 내용 확인 방법 ( 바이트배열을 인코딩하여 문자열로 변환 )
                //Console.WriteLine($"[data] ==> [{buffer}]");
                //Console.WriteLine($"[data] ==> [{buffer.ToString()}]");
                Console.WriteLine($"[data] ==> [{Encoding.UTF8.GetString(buffer)}]");
                Console.WriteLine("[info] -- Finish Recv");
                // 데이터 송신
                retval = clientSock.Send(buffer, 0, buffer.Length, SocketFlags.None);
                Console.WriteLine("[info] -- Finish Send data");
                Thread.Sleep(2000);


                // 접속 종료
                // 클라이언트의 서비스 요청 또는 메세지를 확인하고 접속 종료
                clientSock.Close();
                Console.WriteLine("[info] -- Client closed by Server");
            }
            catch (Exception ex)
            {
                // 디버깅 창을 사용하여 메세지 확인
                Debug.WriteLine(ex.ToString());
                Console.WriteLine(ex.ToString());
            }

            sock.Close();
            Console.WriteLine("[info] -- Server closed");
        }
    }
}
/*
지나갈 수 있는 구멍이 뚤려있는 겁나 큰 시계가 길을 막고있음
구멍을 분침이 막고있음, 덩그러니 남겨진 건전지를 끼워넣어서 치우기

절벽 위에서 떨어뜨리기



 
 
 
 
 
 
 
 
 
 */