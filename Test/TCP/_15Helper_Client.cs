using System.Net.Sockets;
using System.Text;

namespace console_tcpHelper_Client
{
    internal class Helper_Client
    {
        private static readonly int remotePortNo = 26000;

        static void Main(string[] args)
        {
            // C# 사이트에 제공중인
            // TcpClinet 클래스  예제를 복사하여 상황에 맞게 변경
            Console.WriteLine("TcpClient HelperClass TEST");
            Connect("127.0.0.1", remotePortNo, "TcpClient HelperClass TEST\r\n");
        }

        static void Connect(String server,int portNo, String message)
        {
            try
            {
                // using은 자원 자동 해제, 객체 사용이 끝나면 Dispose가 호출됨
                // 생성자 구동과 함께 서버에 접속
                using TcpClient client = new TcpClient(server, portNo);

                // 데이터 바이트 배열로 변경
                Byte[] data = Encoding.UTF8.GetBytes(message);

                // 스트림 객체를 사용
                NetworkStream ns = client.GetStream();
                // 스트림을 사용하여 데이터 전송
                // 바이트 배열을 전송하는 것은 큰 차이가 없어보이나
                // 다른 스트림 객체와 사용하기 편해짐
                ns.Write(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", message);

                // Receive the server response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // 스트림을 파일에 저장 하기 위한 임시 코드구간
                using (FileStream fs = File.Create(@".\testLog.txt"))
                {
                    while(true)
                    {       
                        // 수신 데이터를 읽어서 처리
                        // 데이터가 수신될때까지 블록됨.
                        Int32 bytes = ns.Read(data, 0, data.Length);
                        if (bytes <= 0) break;

                        responseData = Encoding.UTF8.GetString(data, 0, bytes);
                        Console.WriteLine("Received: {0}", responseData);
                        //ns.Read(data, 0, data.Length);
                        fs.Write(data);
                    }

                    
                }

                // Explicit close is not necessary since TcpClient.Dispose() will be
                // called automatically.
                // 명시적으로 close를 호출하지 않아도 TcpClient.Dispose() 가 자동으로 호출
                // stream.Close();
                // client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }
    }
}
