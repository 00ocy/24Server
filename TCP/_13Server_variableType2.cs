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

                // 통합 배송도 데이터 사이즈를 먼저 추출하고 
                // 나머지 데이터를 수신하도록 함
                Console.WriteLine();
                Console.WriteLine("[info2] ==> RECV waiting..3 size+data");
                sizeBuf = new byte[sizeof(int)];
                retSizeVal = clientSock.Receive(sizeBuf, 0, sizeBuf.Length, SocketFlags.None);
                Console.Write($"[RECV-RAW2] --> recvSizeVal [ {retSizeVal}]Bytes ==> ");
                realDataSize = BitConverter.ToInt32(sizeBuf);
                Console.WriteLine($" Data_size are [ {realDataSize}] Bytes"); 
                // 사이즈 만큼의 데이터 버퍼를 준비
                dataBuf = new byte[realDataSize];
                //int Total_retval = realDataSize;
                int sum_DataSize = 0;

                Console.WriteLine("[info2] ==> RECV Processing..4 Data part");
                while (sum_DataSize < realDataSize)
                {
                    // temp 버퍼는 추가 로 수신되는 데이터를 위한 버퍼
                    // 수신된 데이터를 dataBuf에 연속적으로 기록함.
                    // 이해를 위해서 두개의 버퍼를 사용하였지만
                    // dataBuf를 한개만 사용해서 src.offset 위치를 변경하면서 수신을 하여도 됨.
                    // ex)
                    // retDataVal = clientSock.Receive(dataBuf, sum_DataSize, dataBuf.Length, SocketFlags.None);
                    // 
                    //1.  임시 버퍼 추가로 사용할때 (수업중 사용)
                    //byte[] temp = new byte[1500];
                    //retDataVal = clientSock.Receive(temp, 0, temp.Length, SocketFlags.None);
                    //Console.WriteLine($"[RECV-RAW2] --> recvBytes[ {retDataVal}]");
                    //Console.WriteLine($"[RECV-DATA2] --> [{Encoding.UTF8.GetString(temp)}]");
                    //// 수신된 데이터를 임시 데이터 버퍼안에 누적해서 기록
                    //// 기록 위치를 변경하기위해서 수신 데이터 사이즈를 누적하여 변경 처리
                    //// sum_DataSize 가 offset 위치가 되면 계속 증가하는 형태임.
                    //Array.Copy(temp, 0, dataBuf, sum_DataSize, retDataVal);

                    // 2. 임시 버퍼 없이 dataBuf 사용할때
                    // offset을 수신된 위치만큼 증가시키고
                    // 데이터수신 크기는 남은 양만큼만 요청
                    retDataVal = clientSock.Receive(dataBuf, sum_DataSize, (dataBuf.Length - sum_DataSize), SocketFlags.None);
                    Console.WriteLine($"[RECV-RAW2] --> recvBytes[ {retDataVal}]");
                    Console.WriteLine($"[RECV-DATA2] --> [{Encoding.UTF8.GetString(dataBuf,sum_DataSize,retDataVal)}]");
                    
                    // 공통 부분 사이즈 카운팅
                    sum_DataSize += retDataVal;
                }

                Console.WriteLine($"[RECV-Full] --> [{Encoding.UTF8.GetString(dataBuf)}]");

            }
        }
    }
}
