using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpClient_fixedType
{
    internal class Client_FixedType
    {
        static readonly int FIXTYPE_BUFSIZE = 40;
        static void Main(string[] args)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 25000);
            sendDataWithFixedBuffer(remoteEP);  
        }

        private static void sendDataWithFixedBuffer(IPEndPoint remoteEP)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(remoteEP);

			// 송수신을 위해서 고정된 사이즈로 버퍼 사용
			// 수신시 버퍼를 충분히 가져도 되지만 40바이트 단위로 전송하기로 하였기에 40바이트씩 잘라서 처리				
			// 데이터가 밀리거나 손실이 발생하면 40바이트씩 처리하는 데이터에 오류가 발생할 수도 있다
            Byte[] sendBuffer = new Byte[FIXTYPE_BUFSIZE];
			// 빈공간을 쉽게 확인하기 위한 잔여 공간 처리
			// 반드시 필요한 작업은 아니며, 사이즈 확인을 위해서 넣음
			// 개행문자등 문장 마지막을 인식하기 위해서 사용해도 괜찬을 것같음.
            for (int i = 0; i < FIXTYPE_BUFSIZE; i++)
            {
                sendBuffer[i] = (byte)'#';
            }
			
			// 전달될 요청 메세지 명령 GET, 요청 데이터 Weather/Temp 
            String sendCommand = "GET Weather/Temp";
            Byte[] cmdBuff = Encoding.UTF8.GetBytes(sendCommand);

			// 인코딩 클래스로 변환된 바이트 배열은 명령어의 크기만큼만 배열로 반환됨
			// 40바이트의 고정된 크기의 배열에 데이터를 복사하여 전송 준비함
			// Array 복사할때 source, destination, source의 길이를 정확히 넣어야 함.
            Array.Copy(cmdBuff, sendBuffer, cmdBuff.Length);
			
			// 데이터 전송
            sock.Send(sendBuffer, 0, sendBuffer.Length, SocketFlags.None);

			// 특별한 행동 없이 종료함
			// 필요시 반복처리하는 형태로 변형 가능함.
            sock.Close();
        }
    }
}
