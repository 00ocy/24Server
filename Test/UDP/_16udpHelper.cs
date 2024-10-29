using System.Net;
using System.Net.Sockets;
using System.Text;


namespace console_udpHelper_Client
{
    internal class Program
    {
        private static readonly int recvPort = 25010;

        static void Main(string[] args)
        {
            // 소켓 클래스를 사용한  UDP 통신 loopback 또는 다른 컴퓨터
            // loopback 테스트용 주소설정 방법 두가지
            var remoteEP = new IPEndPoint(IPAddress.Loopback, 25000);
            //var remoteforsend = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);
            //// 다른 컴퓨터 테스트용 (자기 컴퓨터 IP확인 하여 적절한 IP 사용하기)
            //var remoteforsend = new IPEndPoint(IPAddress.Parse("xxx.xxx.xxx.xxx"), 25000);

            // C# UdpClient 클래스 사이트에 제공되는 예제로 사용하여 상황에 맞게 수정함.

            //1. Socket클래스 방식
            //CallSocketUdp(remoteEP);
            //2. Helper 클래스 방식
            //CallHelperUdp(remoteEP);
            //2-1. Connect 미사용 방식
            //CallHelperUdpWithoutConnect(remoteEP);
            //3. 서버, 데이터 수신 
            //CallHelperUdpRecv(recvPort);
            //4-1. 멀티캐스트 송신
            // 내부에 while 구문이 있으니  송신과 수신은 따로 실행해야 함.
            // 프로젝트가 분리되지 않았으니 별도의 프로젝트를 만들어 실행하면 됨.
            // 멀티캐스트 주소가 가상의 서버처럼 동작하므로 주소가 같아야 수신됨.
            //CallHelperMulticast("224.0.0.2", 25000);
            //4-2. 멀티캐스트 수신
            CallHelperMulticastRecv("224.0.0.2", 25000);

        }

        private static void CallHelperMulticastRecv(string multicastAddress, int multicastPort)
        {
            UdpClient udpClient = new UdpClient();
            Console.WriteLine("we are ready to recv from Multicast");
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, multicastPort);
            udpClient.Client.Bind(localEP);
            udpClient.JoinMulticastGroup(IPAddress.Parse(multicastAddress));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                byte[] buffer = udpClient.Receive(ref remoteEP);
                string returnData = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"[dataRecv]->[{returnData.Length}]bytes ::{returnData}");
            }
        }

        private static void CallHelperMulticast(string multicastAddress, int multicstPort)
        {
            UdpClient udpClient = new UdpClient();
            IPEndPoint multicastEP = new IPEndPoint(IPAddress.Parse(multicastAddress), multicstPort);

            while (true)
            {
                byte[] buffer = Encoding.UTF8.GetBytes("Hello UdpClient multicast TEST1 with MASTER-PC");
                udpClient.Send(buffer, buffer.Length, multicastEP);
                Thread.Sleep(1000);
                Console.WriteLine($"we sending data to {multicastEP.ToString()}");
            }
        }

        private static void CallHelperUdpRecv(int portNo)
        {
            UdpClient udpClient = new UdpClient(portNo);
            Console.WriteLine($"[info] -- we waiting until RecvData in {portNo}");

            //수신자 검출용
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                if (udpClient.Available <= 0)
                {
                    Thread.Sleep(1000);
                    // 다른 작업중 한번씩 검사하여 확인함.
                    Console.WriteLine("We waiting data from you......");
                }
                else
                {
                    Console.WriteLine($"We waiting recv-data ");
                    Byte[] buffer = udpClient.Receive(ref remoteEP);
                    string realData = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine("We recived Message " + realData.ToString());
                    Console.WriteLine($"This message was sent from {remoteEP.ToString()}");
                }
            }

        }

        private static void CallHelperUdpWithoutConnect(IPEndPoint remoteEP)
        {
            Console.WriteLine("[info] -- withoutConnect Type TEST");
            UdpClient udpClient = new UdpClient(25002);
            // Sends a message to the host to which you have connected.
            Byte[] sendBytes1 = Encoding.UTF8.GetBytes("Socket UDP Helper Class without connect TEST11~!\r\n");
            Byte[] sendBytes2 = Encoding.UTF8.GetBytes("Socket UDP Helper Class without connect TEST222~!\r\n");
            Byte[] sendBytes3 = Encoding.UTF8.GetBytes("Socket UDP Helper Class without connect TEST333~!\r\n");
            Byte[] sendBytes4 = Encoding.UTF8.GetBytes("Socket UDP Helper Class inner socket TEST444~!\r\n");

            udpClient.Send(sendBytes1, sendBytes1.Length, remoteEP);
            udpClient.Send(sendBytes2, sendBytes2.Length, remoteEP);
            Console.WriteLine("[info] -- Send IPEndpoint change to string type 1 local IP address");
            udpClient.Send(sendBytes3, sendBytes3.Length, "127.0.0.1", remoteEP.Port);
            Console.WriteLine("[info] -- Send IPEndpoint change to string type 2 url");
            udpClient.Send(sendBytes3, sendBytes3.Length, "www.google.com", remoteEP.Port);
            Console.WriteLine("[info] -- Send with inner Socket");
            udpClient.Client.SendTo(sendBytes4, remoteEP);

            udpClient.Close();
        }

        private static void CallHelperUdp(IPEndPoint remoteEP)
        {
            Console.WriteLine("[info] -- withConnect Type TEST");
            UdpClient udpClient = new UdpClient(25001);
            try
            {
                udpClient.Connect(remoteEP.Address, remoteEP.Port);

                // Sends a message to the host to which you have connected.
                Byte[] sendBytes1 = Encoding.UTF8.GetBytes("Socket UDP Helper Class TEST11~!\r\n");
                Byte[] sendBytes2 = Encoding.UTF8.GetBytes("Socket UDP Helper Class TEST222~!\r\n");

                udpClient.Send(sendBytes1, sendBytes1.Length);
                udpClient.Send(sendBytes2, sendBytes2.Length);

                udpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void CallSocketUdp(IPEndPoint remoteEP)
        {
            try
            {
                Socket remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // 클라이언트 모드에서 옵션이므로 주석처리하면 동적 할당된 포트를 사용함
                // 바인딩하면 해당 포트 번호를 사용
                // 서버처럼 해당 포트를 사용하고 데이터 수신을 대기와 같은 상태
                remoteSocket.Bind(new IPEndPoint(IPAddress.Any, 25001));


                string data = "You must study network ~!!!!!!";
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                // NullReferenceException을 위해서 버퍼를 null 변경하고 실행
                //byte[] buffer = null;
                //remoteSocket.SendTo(buffer, buffer.Length, SocketFlags.None, remoteEP);

                //var remote1 = new IPEndPoint(IPAddress.Parse("172.18.27.255"), 25000);
                //remoteSocket.SendTo(buffer, buffer.Length, SocketFlags.None, remote1);

                // 수신된 패킷의 발송자 정보를 얻기 위한 데이터객체 생성
                // IPEndPoint 에 생성자는 반드시 주소와 포트를 적어야하기에 의미없는 내용으로 채움
                var receiver = new IPEndPoint(IPAddress.Any, 0);
                // ReceiveFrom 함수의 2번째 인자가 EndPoint이며 ref 타입이여서 타입캐스팅후에 전달
                EndPoint remoteForReceive = (EndPoint)receiver;

                // 지속적으로 데이터를 받기 위한 루프
                while (true)
                {
                    // UDP 수신 최소 사이즈 보내는 데이터가 크면 예외 발생
                    // 소켓이 수신된 데이터 모두를 한번에 전달
                    //byte[] buffer2 = new byte[1600];
                    // LoopBack에서 분할 없이 한번에 전송됨
                    // 테스트시 6KB 전송 하면 수신시 6KB 보다 큰 버퍼가 필요함
                    byte[] buffer2 = new byte[1024*7]; // 7168 Bytes
                    Console.WriteLine("Before recevFrom ~~");
                    // 동기식 함수로 데이터 수신이 없으면 Blocking 상태를 유지하고 더이상 코드가 진행되지 않는다
                    int recvbyte = remoteSocket.ReceiveFrom(buffer2, ref remoteForReceive);
                    Console.WriteLine($"after recevFrom [{recvbyte}]bytes");

                    // UDP는 연결이 끝났는지 모른다.
                    // 데이터가 없는 패킷을 보냈을때만 반응
                    if (recvbyte == 0)
                    {
                        Console.WriteLine("Closed from server~!");
                        return;
                    }
                }

            }
            catch (NullReferenceException ex)
            {
                //발송 데이터가 Null 일때
                Console.WriteLine(ex.ToString());
            }
            catch (SocketException ex)
            {
                // 소켓 설정 에러 or 바인딩 실패
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Closed from localhost~!");
            return;
        }
    }
}
