using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lean
{
    internal class Program
    {
        private static Socket socket;
        static void Main(string[] args)
        {
            // 지정한 ip로 설정한 nic 또는 네트워크로 수신되는 데이터
            var localEP = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 25000);
            // 루프백 주소로 수신되는 데이터, 외부 데이터 X
            var localEPLoopBack = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);
            localEPLoopBack = new IPEndPoint(IPAddress.Loopback, 25000);
            // 호스트가 소유한 모든 nic에서 수신되는 데이터
            var localEPALL = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 25000);
            localEPALL = new IPEndPoint(IPAddress.Any, 25000);

            // 로컬 EndPoint로 설정된 서버 시작
            StartServer(localEPALL);
        }

        private static void StartServer(IPEndPoint localEPALL)
        {
            try
            {
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,  ProtocolType.Tcp);

            // EndPoint는 추상 클래스. 운영체제에 리소스 사용 요청. 중복 시 예외
            Console.WriteLine("[info] -- ServerBinding");
            // 바인드. 아이피랑 포트번호 묶기
            socket.Bind(localEPALL);
            // 둘 다 정보 표시
            Console.WriteLine($"[info] -- Server Bind to [{localEPALL.Address}] : [{localEPALL.Port}]");
            Console.WriteLine($"[info] -- Server Bind to [{socket.LocalEndPoint}]");

            // 서버 시작. 접속 신호 수신 및 대기열 생성
            Console.WriteLine("[info] -- Server Listenning with limit 10");
            socket.Listen(10);
            Console.WriteLine("[info] -- Start Server");


            // 동기식 함수로 운영체제로 권한이 넘어가 대기상태로 전환
            // 접속자가 있을때 Accept 함수가 소켓 정보를 반환
            // 반환된 소켓은 접속자와 통신할 수 있는 소켓이며,
            // 서버가 Listen 을 위해 사용하는 소켓과 구별 됨.
            Console.WriteLine("[info] -- Server blocking in Accept()");
            var clientSock = socket.Accept();
            Console.WriteLine("[info] -- Client Connected");
            // IPEndPoint 가져오기, EndPoint 형태여서 캐스팅 해줘야 함
            IPEndPoint clientEP = (IPEndPoint)clientSock.RemoteEndPoint;
            // 둘 다 정보 표시
            Console.WriteLine($"[Client] -- from [{clientEP.Address}] : [{clientEP.Port}]");
            Console.WriteLine($"[Client] -- from [{clientSock.RemoteEndPoint}]");

            // 데이터 송수신 (반환된 클라이언트 소켓 사용)
            Console.WriteLine("[data] ==> transfer data from client to server");
            // 데이터 수신
            byte[] buffer = new byte[1500];
            int retval = clientSock.Receive(buffer,0,buffer.Length, SocketFlags.None);
            Console.WriteLine($"[info] -- recvBytes[{retval}]");


            // 주의 버퍼 내용 확인 방법 (바이트 배열을 인코딩하여 문자열로 변환)
            Console.WriteLine($"[data] ==> [{Encoding.UTF8.GetString(buffer)}]");
            Console.WriteLine("[info] -- Finish Recv");
            // 데이터 송신
            retval = clientSock.Send(buffer, 0, buffer.Length, SocketFlags.None);
            Console.WriteLine("[info] -- Finish Send data");
            Thread.Sleep(6000);

            // 접속 종료
            // 클라이언트의 서비스 요청 또는 메세지를 확인하고 접속 종료
            clientSock.Close();
            Console.WriteLine("[info] -- Client closed by Server");
            }
            catch(Exception ex) 
            {
                // 디버깅 창을 사용하여 메세지 확인
                Debug.WriteLine(ex.ToString());
                Console.WriteLine(ex.ToString());
            }

            socket.Close();
            Console.WriteLine("[info] -- Server closed");

        }
    }
}