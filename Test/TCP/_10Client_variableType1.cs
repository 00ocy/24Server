using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpClient_variableType1
{
    internal class Client_variableType1
    {
        // 메세지 끝을 판단하기 위한 문자또는 문자열 사용
        static readonly string terminalStr = "\n";
        static readonly string terminalStr2 = "[[ETX]]";

        static void Main(string[] args)
        {
            Console.WriteLine("[CLIENT] Send Message VariableType 1 with TerminalChar"); 
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 26000);

            // 끝문자를 글로벌 변수로 사용하였지만, 함수 인자로 전달 하도록 작성하여도 상관없음
            SendMessageVariableTypeWithTerminalChar(remoteEP);
        }

        private static void SendMessageVariableTypeWithTerminalChar(IPEndPoint remoteEP)
        {
            // 소켓 생성
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 서버 접속
            sock.Connect(remoteEP);

            // 데이터 발송 준비
            // 전달 메세지 
            String sendCommand = "GET Weather/NewTemp";
            // 전달 메세지 바이트 배열로 변경
            Byte[] cmdBuff = Encoding.UTF8.GetBytes(sendCommand);
            // Array.Copy를 이용한 문자열 붙여넣기
            // 다음 두가지를 사용할 예정
            // Array.Copy(src, dst, length)
            // Array.Copy(src, src.offset, dst, dst.offset, length)
                

            // 전달 메세지의 끝 문자열 붙여넣기
            // 2. ETX 문자열 사용
            byte[] terminalBufStr = Encoding.UTF8.GetBytes(terminalStr2);
            
            // 샌드 버퍼 = 메세지 + 끝문자 버퍼 길이
            Byte[] sendBuffer = new byte[cmdBuff.Length + terminalBufStr.Length];
            // 터미널 버퍼를, 0번째부터, 샌드버퍼에 복사, 메세지길이, 터미널 버퍼 길이까지
            Array.Copy(terminalBufStr, 0, sendBuffer, cmdBuff.Length, terminalBufStr.Length);
            // 메세지 버퍼를, 샌드 버퍼에다가, 메세지 버퍼 길이만큼 복사
            Array.Copy(cmdBuff, sendBuffer, cmdBuff.Length);

            //명령어 바이트배열을 전송용 유저버퍼에 기록
            //명령어를 먼저 기록해도 되고 끝문자를 기록해도 됨.
            // 버퍼를 생성하면서 편한데로 작성 , 단 수업중에는 명령어를 먼저 기록해봤음.

            //데이터 전송
            sock.Send(sendBuffer, 0, sendBuffer.Length, SocketFlags.None);

            sock.Close();
        }
    }
}
