using System.Net.Sockets;
using System.Net;
using System.Text;

namespace console_tcpHelper_Listener
{
    internal class Helper_Listener
    {
        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                // C# 사이트에 제공중인
                // TcpListener 클래스  예제를 복사하여 상황에 맞게 변경

                Int32 port = 26010;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // 서버 객체 생성과 기본 정보 입력
                server = new TcpListener(localAddr, port);

                // 서버의 시작
                server.Start();

                // 데이터 준비
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    using TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

                    // 내부 Socket 객체를 사용하지 않고
                    // 스트림 객체를 사용하는 것이 특징
                    NetworkStream stream = client.GetStream();
                    // 내부 Socket  객체를 이용하는 예시
                    //client.Client.Send(Encoding.UTF8.GetBytes("testsetset"), 0, 10);
                    int i;

                    // 접속한 클라이어트로 데이터가 들어올때가지 블록됨
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // 수신 데이터 확인
                        data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);

                        // 수신 데이터 대문자로 변경
                        data = data.ToUpper();

                        // 바이트 배열로 변경
                        byte[] msg = System.Text.Encoding.UTF8.GetBytes(data);

                        // 데이터 전송 에코 처리 
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0}", data);
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // 최종 서버 정지
                // finally 구간은 예외 발생 및 정상처리 이후에도 반드시 처리되는 곳
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
    }
}
