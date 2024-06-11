using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpServer_fixedType
{
    internal class Program
    {
        static readonly int FIXEDTYPE_BUFSIZE = 40;
        static void Main(string[] args)
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 25000);
            StartServerWithFixedType(localEP);
        }

        private static void StartServerWithFixedType(IPEndPoint localEP)
        {
			// 수업중 시간 부족으로 핵심 내용만 작성
            Socket listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSock.Bind(localEP);
            listenSock.Listen();

            Console.WriteLine("[info] -- Sever Start");
            while(true) 
            { 
                Socket clientSock = listenSock.Accept();
                IPEndPoint clientEP = (IPEndPoint)clientSock.RemoteEndPoint;
                Console.WriteLine($"[info] -- connection with [{clientEP.Address}]:[{clientEP.Port}]");
				
				// 송수신을 위해서 고정된 사이즈로 버퍼 사용
				// 수신시 버퍼를 충분히 가져도 되지만 40바이트 단위로 전송하기로 하였기에 40바이트씩 잘라서 처리				
				// 데이터가 밀리거나 손실이 발생하면 40바이트씩 처리하는 데이터에 오류가 발생할 수도 있다
                byte[] buffer = new byte[FIXEDTYPE_BUFSIZE];

                Console.WriteLine("[info] -- RECV waiting");
                int retval = clientSock.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                Console.WriteLine($"[RECV] --> recvBytes[{retval}]");
                Console.WriteLine($"[RECV] --> [{Encoding.UTF8.GetString(buffer)}]");
                
                // 구문분석 및 처리
				// 에러 처리				
                // 응답처리

                // 소켓 닫기
                clientSock.Close();
            }
        }
    }
}
