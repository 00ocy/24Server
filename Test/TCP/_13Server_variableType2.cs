using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpServer_variableType2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[SERVER] Send Message VariableType 2 With SizeData");
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 25000);
            StartServerVariableTypeWithSizeData(localEP);
        }

        private static void StartServerVariableTypeWithSizeData(IPEndPoint localEP)
        {
            // Accept 호출되는 구간까지 모두 동일
            Socket listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 지연 시뮬레이션을 위해서 수신 버퍼 크기 변경
            // 하지만 로컬  루프백으로 테스트하면 효과가 없음
            // 고속으로 전송됨. < 100ns 이네
            // 지연 현상을 시뮬레이션하여 데이터 결합을 처리하고 싶다면 송신측에서 버퍼를 분할하여 천천히 전송할 것
            listenSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 15);
            listenSock.Bind(localEP);
            listenSock.Listen();

            Console.WriteLine("[info] -- Sever Start");

            while (true)
            {
                Socket clientSock = listenSock.Accept();
                // 주의 서버 접속은 listenSock으로 하고 접속한 클라이언트들은
                // 개별로 클라이언트 소켓으로 반환되어 처리
                IPEndPoint clientEP = (IPEndPoint)clientSock.RemoteEndPoint;
                Console.WriteLine();
                Console.WriteLine($"[info] -- connection with [{clientEP.Address}]:[{clientEP.Port}]");

                // 분리 배송 수신 대기
                Console.WriteLine("[info] ==> RECV waiting..1 sizeData");
                byte[] sizeBuf = new byte[sizeof(int)];
                int retSizeVal = clientSock.Receive(sizeBuf, 0, sizeBuf.Length, SocketFlags.None);
                Console.Write($"[RECV-RAW] --> recvSizeVal [ {retSizeVal}]Bytes ==> ");
                int realDataSize = BitConverter.ToInt32(sizeBuf);
                Console.WriteLine($" Data_size are [ {realDataSize}] Bytes"); ;
                byte[] dataBuf = new byte[realDataSize];
                Console.WriteLine("[info] ==> RECV waiting..2 realData");
                int retDataVal = clientSock.Receive(dataBuf, 0, dataBuf.Length, SocketFlags.None);
                Console.WriteLine($"[RECV-RAW] --> recvBytes[ {retDataVal}]");
                Console.WriteLine($"[RECV-DATA] --> [{Encoding.UTF8.GetString(dataBuf)}]");

       

            }
        }
    }
}
