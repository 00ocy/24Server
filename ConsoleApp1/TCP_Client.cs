using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcp_00
{
    internal class Program
    {
        private static Socket sock;

        static void Main(string[] args)
        {
            // 1. 로컬 네트워크 테스트용 EndPoint 설정
            var remoteEP = new IPEndPoint(IPAddress.Parse("172.18.27.70"), 25000);
            // TCP 클라이언트로 메세지 전송
            SendTcpMsg(remoteEP, "Hello First App\n");

            //// 2. DNS 를 이용한 네트워크 전송 설정
            //var hostEntry = Dns.GetHostEntry("www.google.com");
            //// HostEntry.AddressList 에서 확인된 IP 정보로 EndPoint 설정
            //var remoteEP = new IPEndPoint(hostEntry.AddressList[0], 80);
            //// TCP 클라이언트로 메세지 전송
            //// 차이점 웹서버가 인식가능한 문장으로 전송
            //SendTcpMsg(remoteEP, "GET / HTTP/1.1\r\n\r\n");
        }

        private static void SendTcpMsg(IPEndPoint remoteEP, string v)
        {
            try
            {
                // Socket 생성
                Console.WriteLine("[info] -- Create Socket");
                sock = new Socket(AddressFamily.InterNetwork, 
                                    SocketType.Stream, ProtocolType.Tcp);

                // Socket 접속 시도
                Console.WriteLine($"[info] -- Connect to [{remoteEP.Address}]:[{remoteEP.Port}]");
                sock.Connect(remoteEP);
                Console.WriteLine("[info] -- Connected !!");

                // 데이터 준비, 변경 string to Byte[]
                //Console.WriteLine(v);
                byte[] buffer = Encoding.UTF8.GetBytes(v);
                int size = buffer.Length;

                // 버퍼 내용 확인 
                // String 클래스와 다르게 바이트배열은 클래스가 가지고 있는 기본 정보는 타입 정보이며 
                // 인코딩을 통해서 내용을 확인가능
                //Console.WriteLine($"[data] ==> [{buffer}]");
                //Console.WriteLine($"[data] ==> [{buffer.ToString()}]");
                Console.WriteLine($"[data] ==> [{Encoding.UTF8.GetString(buffer)}]");

                // 데이터 송신
                int retval = sock.Send(buffer, 0, size, SocketFlags.None);
                Console.WriteLine("[info] -- Finish Send data");

                // 데이터 수신
                // 이더넷 최대 데이터 사이즈 MTU == 1500Bytes
                // TCP에서는 작아도 문제는 없음.
                // UDP에서는 수신 버퍼 사이즈가 작으면 예외가 발생
                buffer = new byte[1500];
                retval = sock.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                Console.WriteLine($"[info] -- recvBytes[{retval}]");

                // 데이터 처리, 바이트 확인, 수정, 삭제 등등
                for( int i = 0; i < retval; i++ )
                {
                    if (buffer[i] == 0x0d || buffer[i] == 0x0a)
                    {
                        buffer[i] = 0x21;
                    }
                }

                // 수신 데이터 최종 확인 
                //Console.WriteLine($"[data] ==> [{buffer}]");
                //Console.WriteLine($"[data] ==> [{buffer.ToString()}]");
                Console.WriteLine($"[data] ==> [{Encoding.UTF8.GetString(buffer)}]");
                Console.WriteLine("[info] -- Finish Recv");
            }
            catch (Exception ex)
            {
                // 디버거의 출력창을 사용한 메세지 확인
                //Debug.WriteLine(ex.ToString());
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("[info] -- socket closed");
            sock.Close();
        }
    }
}
