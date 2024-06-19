using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpServer_variableType1
{
    internal class Program
    {
        // 메세지 끝을 판단하기 위한 문자또는 문자열 사용
        static readonly string terminalStr2 = "[[ETX]]";

        static void Main(string[] args)
        {
            Console.WriteLine("[SERVER] Send Message VariableType 1 with TerminalChar");
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 26000);
            StartServerVariableTypeWithTerminalChar(localEP);
        }

        private static void StartServerVariableTypeWithTerminalChar(IPEndPoint localEP)
        {
            Socket listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 15);
            listenSock.Bind(localEP);
            listenSock.Listen();

            Console.WriteLine("[info] -- Sever Start");
            while (true)
            {
                Socket clientSock = listenSock.Accept();
                IPEndPoint clientEP = (IPEndPoint)clientSock.RemoteEndPoint;
                Console.WriteLine($"[info] -- connection with [{clientEP.Address}]:[{clientEP.Port}]");
                // 버퍼 사용시 주의 사항
                // 사전에 1500만큼 배열을 생성하면 
                // Receive 함수이후에 실제 데이터 포함하고 나머지는 null 로 채워진
                // 1500바이트 배열을 모두 사용하게 되므로 
                // 필요한 데이터 사이즈 만큼 바이트 배열에서 추출하여 사용한다.
                byte[] buffer = new byte[1500];

                Console.WriteLine("[info] -- RECV waiting");
                int retval = clientSock.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                Console.WriteLine($"[RECV-RAW] --> recvBytes[{retval}]");
                string temp = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"[RECV-RAW] --> [{temp}]");
                
                // 수신데이터 처리
                // 끝문자열로 메세지 끝 확인
                // 1. string 객체에 끝문자열이 있는지 확인하고 처리하는 방법
                // 2. string 객체에서 indexOf 함수로 바로 처리하는 방법의 경우 '-1' 반환상태를 검사
                if (temp.Contains(terminalStr2))
                {
                    // 끝문자열 위치 확인
                    int idx = temp.IndexOf(terminalStr2);
                    // 추출할 실제 데이터 공간 준비
                    // 이미 string 객체로 변경한 temp 를 사용해서 substring으로 추출해도 됨.
                    // 다른 형태의 데이터를 받았다고 가정하고 바이트 배열에서 복사하여 추출함.
                    //byte[] data = new byte[buffer.Length];
                    byte[] data = new byte[idx];
                    // 사용자 수신버퍼에서 data버퍼로 끝문자위치까지 복사
                    Array.Copy(buffer, 0, data, 0, idx);
                    Console.WriteLine($"[RECV-DATA] --> [{Encoding.UTF8.GetString(data)}]");
                }

                // 구문분석 및 처리
                // 에러 처리				
                // 응답처리

                // 소켓 닫기
                clientSock.Close();
            }
        }
    }
}
