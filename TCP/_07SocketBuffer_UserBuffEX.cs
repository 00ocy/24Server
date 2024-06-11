using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcp_03
{
    internal class Program
    {
        private static Socket socket;

        static void Main(string[] args)
        {
            //var remoteEP = new IPEndPoint(IPAddress.Parse("172.18.27.70"), 25000);
            //SendTcpMsg(remoteEP, "Hello First App\n");
			
			// 샘플 대량의 데이터를 받기위해 구글 웹서버 이용
            var hostEntry = Dns.GetHostEntry("www.google.com");
            var remoteEP = new IPEndPoint(hostEntry.AddressList[0], 80);
            Console.WriteLine($"Send EndPoint [{hostEntry.AddressList[0].ToString()}]");
			// HTTP 웹페이 요청 명령 헤더
            SendTcpMsg(remoteEP, "GET / HTTP/1.1\r\n\r\n");
        }

        private static void SendTcpMsg(IPEndPoint remoteEP, string v)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //// 소켓 버퍼 설정 및 확인
				//// 소켓 수신 버퍼 설정 변경 ==> 송신 버퍼도 동일하게 변경 가능함.
                int optval;
                optval = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                Console.WriteLine("소켓 수신 버퍼 크기(old) = {0}바이트", optval);

                // Wireshark 패킷의 windows size  상태 변화 확인
				// 사이트 접속시 수신 버퍼의 사이즈가 300이라고 알려주고 전송 요청함
				// 보내는 패킷의 크기를 300에 맞춰서 보냄
                optval = 300;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, optval);
                // 수신 버퍼의 크기를 얻는다.
                optval = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                Console.WriteLine("소켓 수신 버퍼 크기(new) = {0}바이트", optval);

                socket.Connect(remoteEP); 
                Console.WriteLine($"[info] -- connection to Server " + $"{remoteEP.Address}:{remoteEP.Port}");

                // 데이터변경 string to Byte[]
                byte[] buffer = Encoding.UTF8.GetBytes(v);
                int size = buffer.Length;
                int sizeSum = 0;

                // 데이터 송신
				// 웹서버에 페이지 요청
                Console.WriteLine(v);
                int retval = socket.Send(buffer, 0, size, SocketFlags.None);
                Console.WriteLine("[info] -- send finish~!");

				// 사용자 버퍼의 사용
                // 1. 수신 대기 없이 바로 확인 ( 현재까지 받은 데이터만 반환 )
                //buffer = new byte[1500];    // 부족함. 패킷이 2~3번정도 도착하였음.
                //buffer = new byte[3000];    // 부족하지 않았으며, 전송된 데이터가 더이상 없음
                //buffer = new byte[100_000]; // 부족하지 않았으며, 전송된 과도한 데이터 공간 낭비가 발생
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                // 2. 수신 대기 없이 반복 호출하여 데이터 취득
				// 사용자 버퍼의 크기와 상관 없이 수신된 패킷 데이터 까지 만 전달 받음
				// 패킷이 모두 도착하지 않았으며 현재까지 전송된 데이터만 처리됨
                //buffer = new byte[1500];
                //buffer = new byte[60_000];  // 적절한 사이즈였으며, 수신되지 않은 패킷이 있었음.
                //buffer = new byte[10];	  // 소켓 버퍼에 수신된 데이터가 많았지만 사용자 버퍼 사이즈가 너무 작음
                //buffer = new byte[3000];    // 부족하지 않았으며, 직전에 받아가 데이터를 제외한 소켓버퍼에 남은 데이터 수신,
                //buffer = new byte[100_000]; // 부족하지 않았으며, 전송된 과도한 데이터 공간 낭비가 발생
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                //Console.WriteLine($"[info] -- recvBytes[{retval}]");
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                //Console.WriteLine($"[info] -- recvBytes[{retval}]");
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                //Console.WriteLine($"[info] -- recvBytes[{retval}]");
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                //Console.WriteLine($"[info] -- recvBytes[{retval}]");
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
				
                // 데이터 수신 지연 시뮬레이션
                // 3. 수신 대기 2초
                //Console.WriteLine("[info] -- Waiting Timer 2sec !");
				// 모든 데이터 패킷이 수신될때까지 2초정도 걸린다고 예상하고 대기함
                //Thread.Sleep(2000);
                //buffer = new byte[100_000]; // 부족하지 않았으나 전송된 과도한 데이터 공간 낭비가 발생
				//						      // 모든 패킷이 전송되어 사용자 버퍼로 전달 받음
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);


                // 3. 반복문 사용 
				// 탈출조건으로 시간을 사용하기 위한 타임객체
                DateTime startTime = DateTime.Now;
                DateTime endTime = DateTime.Now;
                while (true)
                {
                    // 데이터 수신
                    // 사용자 버퍼 사이즈 변경
                    //buffer = new byte[300_000];
                    buffer = new byte[1500];
                    //buffer = new byte[10];
                    retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    //Console.WriteLine($"[info] -- recvBytes[{retval}]");
                    Console.WriteLine($"{Encoding.UTF8.GetString(buffer)}");
                    sizeSum += retval;


                    // 탈출조건 사이즈
                    //if (sizeSum >= 58_000) break;
                    // 탈출조건 시간 (<2초) 시간 지연 시뮬레이션 값 적용
                    endTime = DateTime.Now; 
                    TimeSpan timeSpan = endTime - startTime;
					// 사용자 버퍼크기에 맞춰 버퍼로 읽어오는 시간까지 필요함
					// 사용자가 자주 Receive 함수를 호출하면 2초 이상 필요함.
                    if (timeSpan.TotalSeconds > 2)
					// 소켓버퍼를 극단적으로 10Bytes 까지 줄이면 전체 패킷을 수신하는데 까지 매우 많은 시간이 걸림.. 60초이상
                    //if (timeSpan.TotalSeconds > 600)
                    {
                        Console.WriteLine($"[info] -- Total recv [{sizeSum}]Bytes");
                        break;
                    }
                }


                //데이터 수신
                // 수신 버퍼 사이즈
                //    buffer = new byte[1500];
                //retval = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                //Console.WriteLine($"[info] -- recvBytes[{retval}]");


				//Console.WriteLine($"[data1] ==> [{buffer}]");
				//Console.WriteLine($"[data2] ==> [{buffer.ToString()}]");

				//for(int i =0; i < buffer.Length; i++)
				//{
				//    if(buffer[i] == 0x0d  || buffer[i] == 0x0a )
				//    {
				//        buffer[i] = 0x21;                            
				//    }
				//}

                // Console.WriteLine($"[RECV-DATA] =====================================================");
                // Console.WriteLine($"{Encoding.UTF8.GetString(buffer)}");
                // Console.WriteLine($"[RECV-DATA] =====================================================");
                // Console.WriteLine("[info] -- recv finish~!");
            }
            catch(Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
            }

            socket.Close();
        }
    }
}
