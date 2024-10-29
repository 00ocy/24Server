
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LastNetworkClass
{
    internal class Program
    {
        private static bool recvAliveCtrl = false;
        private static bool sendAliveCtrl;
        private static Thread sendThread;
        private static Thread recvThread;
		

        static void Main(string[] args)
        {
            // 예시 2
            // 172.18.27.70:30000 TCP 로 접속하여
            // 학번, 이름, 간단한 메세지 (< 100Bytes) 전송
            // 접속 종료
            // 함수로 작성 성공, 메인함수만 작성하면 감점
            // 클래스로 작성하면 가산점
				SendMessageSingleShot("172.18.27.70", 30001, "TEST Message\n");
            // 예시 2-1
            // 접속하여 데이터 동시에 주고 받기  (Thread 반드시 사용해야만 한다)
				SendMessageWithRecvThread("127.0.0.1", 30000);
	
            // 예시 3
            // 다중접속은 필요없음
            // TCP 서버모드로 27000을 동작시키고 데이터 수신을 받는다.
            // 이때 메세지 형식은 끝문자로 '[[HOME]]' 사용하는 방식
            // 끝문자 단위로 화면에 출력한다. 단, 끝문자는 출력하지 않는다
            
            // 예시 3-1
            // 다중접속 처리
			// 다중접속 처리 유무 설정, (예시를 위해서 사용함)
				//StartServer("0.0.0.0", 27000, true);

            // 예시 4 
            // TCP 서버 모드로 37000 동작시키고,다중접속을 허용합니다.
            // 접속자의 수신 데이터를 {각각의 파일로 저장}=> 로그서버로 전송
            // 접속자 수신 메세지 형식 => [메세지 크기][메세지 본문]
				// 구성 예시 코드
				//var logserver = new LogServer(new IPEndPoint(IPAddress.Parse("224.0.0.10"), 28000));
				//var controlServer = new ControlServer(logserver, "0.0.0.0", 37000);
				//controlServer.Start();

            // 로그 서버로 접속 정보와 메세지를 전송 
            // 로그 서버 주소 224.0.0.10:28000
            // 로그관련 사항
            // 메세지 형식 => [메세지 크기][메세지 본문]
            // 메세지 본문 형식 
            /*
                {
                    "LogTime":"{메세지 수신시간}",
                    "userEP":"{접속자 정보}",
                    "MSG" :"{수신된 메세지}"
                }             
             */
        }

        private static void StartServer(string ipAddr, int port, bool MultiType=false)
        {
            Socket serverSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddr), port);

            Console.WriteLine("[info] Sever starting..");
            
			serverSock.Bind(serverEP);

            Console.WriteLine("[info] Sever Waiting clients..");
            serverSock.Listen();

            while(true)
            {
                Socket clientSock = serverSock.Accept();
                Console.WriteLine($"[info] -- {clientSock.RemoteEndPoint.ToString()} is connected");

                if(MultiType)
                {
                    // Using Multi User, Thread or Task
                    Task.Factory.StartNew(clientProcess, clientSock);
                    
                }
                else
                {
                    // None multi User
                    clientProcess(clientSock);
                }
                // condition for Closing server

            }
            serverSock.Close();
        }

        private static void clientProcess(object clientSock)
        {
            Socket sock = (Socket)clientSock;
            byte[] buffer = new byte[1024];
            int nBytes = 0;

            while(true)
            {
                nBytes = sock.Receive(buffer,0, buffer.Length, SocketFlags.None);
                if( nBytes > 0)
                {
                    string msgString = Encoding.UTF8.GetString(buffer); 
                    Console.WriteLine($"[info-recv] [{nBytes}bytes]{msgString}");
                }
				// 수신 메세지 처리중 외부 연결이 끝났을때
                else if(nBytes <= 0)
                {
                    Console.WriteLine($"{sock.RemoteEndPoint.ToString()} is closed");
                    break;
                }
            }

            sock.Close();
        }

        private static void SendMessageWithRecvThread(string ipAddr, int port)
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddr), port);
            Socket socket = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Stream,
                                        ProtocolType.Tcp);
            Console.WriteLine("[info-Main] Connecting....");
            socket.Connect(serverEP);

            Console.WriteLine("[info-Main] Connected....");
            // 스레드를 동작시킨다
            sendThread = new Thread(sendMessageThread);
            recvThread = new Thread(recvMessageThread);
            sendThread.Start(socket);
            recvThread.Start(socket);
            sendThread.Join();
            recvThread.Join();

            // 다중 접속자가 있는 곳이라면 이곳에서 소켓을 닫지 않고 유저 스레드에서 처리하는게 적절함
            Console.WriteLine("[info-Main] Disposing...");
            Console.WriteLine("[info-Main] socket closing...");
            socket.Close();
        }

        private static void recvMessageThread(object? obj)
        {
            Socket socket = (Socket)obj;  
            recvAliveCtrl = true;
            var count = 0;
            byte[] recvBuff = new byte[1024];
            // 동기시 데이터 수신 함수를 제한 시간 동안만 유지 하고 시간이 지나면 리턴되도록 설정할때 사용함
            // 다만 처리 방식을 지금과 다르게 변경하여야 한다
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 500);

			try
			{
                // 수신 메세지 처리 루프를 끝낼때 (TASK에서는  cancellationtoken 사용)
                while (recvAliveCtrl)
				{
					count = socket.Receive(recvBuff);
					// 수신 메세지 처리중 외부 연결이 끝났을때
					if( count <= 0 )
					{
						Console.WriteLine($"[info] From RemoteUser disconnection.");
                        Console.WriteLine($"[info] Disconnecting..");
                        sendAliveCtrl = false;
                        if( socket.Connected ) socket.Disconnect(true);
                        break;
					}

					Console.WriteLine($"[info-recv] -- " +
						$"[{count}bytes]{Encoding.UTF8.GetString(recvBuff)}");
				}
			}
			// 수신 메세지 스레드를 중지 시킬때
            catch(ThreadInterruptedException ex)
            {
                Console.WriteLine("[info-recv threadEX] "+ ex.Message);
            }
            catch(SocketException ex)
            {
                Console.WriteLine("[info-recv socketEX] " + ex.Message);
            }
            catch (Exception ex) 
            { 
                Console.WriteLine("[info-recv EX] " + ex.ToString());
            }
            finally
            {
                //Console.WriteLine("[info-recv] Disposing...");
                sendThread.Interrupt();
            }
        }

        private static void sendMessageThread(object? obj)
        {
            Socket socket = (Socket)obj;
            sendAliveCtrl = true;
            var count = 0;
            try
            {
                // 송신 메세지 처리 루프를 끝낼때 (TASK에서는  cancellationtoken 사용)
                while (sendAliveCtrl)
                {
                    // ReadLine 에서 무한 대기하지 못하게 막고 사용자 스레드를 적절히 종료시키기 위해서
                    while (Console.KeyAvailable == false) Thread.Sleep(250);

                    var msgString = Console.ReadLine();
                    // 송신 메세지 명령으로 끝낼때
                    if (msgString == "QUIT")
                    {
                        Console.WriteLine("[info] From UserConsole Command.");
                        Console.WriteLine($"[info] Disconnecting..");
                        recvAliveCtrl = false;
                        if (socket.Connected)  socket.Disconnect(true);
                        break;
                    }
                    count = socket.Send(Encoding.UTF8.GetBytes(msgString));
                    Console.WriteLine($"[info-send] -- message send {count} bytes");
                }
            }
            // 송신 메세지 스레드를 중지 시킬때
            catch (ThreadInterruptedException ex)
            {
                Console.WriteLine("[info-send threadEX] " + ex.Message);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("[info-send socketEX] " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[info-send EX] " + ex.ToString());
            }
            finally
            {
                //Console.WriteLine("[info-send] Disposing...");
                recvThread.Interrupt();
            }
        }

        private static void SendMessageSingleShot(string ipAddr, int port, string msgString)
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddr), port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(serverEP);
            socket.Send(Encoding.UTF8.GetBytes(msgString));

            socket.Close();
        }
		// 위 메서드에서 쓰레드와 테스크 사용이 비동기와 매우 유사하다는 것을 간략히 보여주는 예시함수
		// async await  위치를 잘 보면  멀티쓰레드를 사용하는 지점에 가까우며
		// 코드의 흐름을 이해하기도 쉽다.
        /*  
        private async static void SendMessage(string ipAddr, int port, string msgString)
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddr), port);
            Socket socket = new Socket(AddressFamily.InterNetwork, 
                                        SocketType.Stream,
                                        ProtocolType.Tcp); 
            byte[] recvBuffer = new byte[1024];
            socket.Connect(serverEP);
            await socket.SendAsync(Encoding.UTF8.GetBytes(msgString));
            await socket.ReceiveAsync(recvBuffer);
            Console.WriteLine($"[info-recv] -- " +
                    $"{Encoding.UTF8.GetString(recvBuffer)}");

            socket.Close();
        }
		*/
    }
}
