using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpClient_variableType2
{
    internal class Client_variableType2
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[CLIENT] Send Message VariableType 2 With SizeData");
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 25000);
            SendMessageVariableTypeWithSizeData(remoteEP);
        }

        private static void SendMessageVariableTypeWithSizeData(IPEndPoint remoteEP)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 5);

            sock.Connect(remoteEP);

            // 사이즈+ 데이터 분리 전송용
            String sendCommand = "GET Weather/NewTemp";
            byte[] DataBuf = Encoding.UTF8.GetBytes(sendCommand);
            byte[] sizeBuf = new byte[sizeof(int)];
            sizeBuf = BitConverter.GetBytes(DataBuf.Length);

            // 사이즈+데이터 통합 전송용
            String sendCommand2 = "POST seoul/Wind";
            byte[] DataBuf2 = Encoding.UTF8.GetBytes(sendCommand2);
            byte[] sizeBuf2 = new byte[sizeof(int)];
            sizeBuf2 = BitConverter.GetBytes(DataBuf2.Length);
            // 호스트 2 네트워크 바이트 오더 바이트 순서변경
            // 수업중에 필요 없고 모든 컴퓨터가 같은 intel 칩이여서 넘어감.
            // 하지만 꼭 함수  확인할것

            // 이버전은 일반  소켓 테스트 툴로는 확인 불가능
            // 일반 소켓 테스터들은 사이즈 확인 처리부분이 없음.

            // 1.분리전송
            // SizeBuf 전송
            sock.Send(sizeBuf, 0, sizeBuf.Length, SocketFlags.None);
            // DataBuf 전송
            sock.Send(DataBuf, 0, DataBuf.Length, SocketFlags.None);

            // 2.통합전송
            //byte[] fullBytes = new byte[DataBuf2.Length + sizeBuf2.Length];
            //Array.Copy(sizeBuf2, fullBytes, sizeBuf2.Length);
            //Array.Copy(DataBuf2, 0, fullBytes, sizeBuf2.Length, DataBuf2.Length);
            //sock.Send(fullBytes);

            // 수업중 통합 전송 지연 시뮬레이션은
            // 수신측에서 데이터 재결합을 시연하기 위해서 작성한 것으로 
            // 이 코드에서 제외하였음.
            // 2-1. 통합 전송 지연 시뮬레이션
            // 2. 통합 전송부분 주석처리하여 사용바람.
            // 합배송 지연 시뮬레이션 
            byte[] spliteBuf1 = new byte[5];
            byte[] spliteBuf2 = new byte[5];
            byte[] spliteBuf3 = new byte[5];
            byte[] spliteBuf4 = new byte[5];
            Array.Copy(DataBuf2, 0, spliteBuf1, 0, spliteBuf1.Length);
            Array.Copy(DataBuf2, spliteBuf1.Length, spliteBuf2, 0, spliteBuf2.Length);
            Array.Copy(DataBuf2, spliteBuf1.Length * 2, spliteBuf3, 0, spliteBuf3.Length);
            //Array.Copy(DataBuf2, spliteBuf1.Length*3, spliteBuf4, 0, spliteBuf4.Length);

            sock.Send(sizeBuf2, 0, sizeBuf.Length, SocketFlags.None);
            Thread.Sleep(1000); // 데이터 지연 효과
            sock.Send(spliteBuf1);
            Thread.Sleep(1000); // 데이터 지연 효과
            sock.Send(spliteBuf2);
            Thread.Sleep(1000); // 데이터 지연 효과
            sock.Send(spliteBuf3);
            Thread.Sleep(1000);
        }
    }
}
