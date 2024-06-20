
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace _202227030OCY
{
    internal class Program
    {
        private static string EndStr = "\n";
        private static string EndStr2 = "[ETX]";
        static void Main(string[] args)
        {
            // 문제 1 UDP서버 포트 12000을 동작
            // 수신되는 메시지 중에서
            // 172.18.27.xxx 호스트의 메시지를 걸러서 출력
            // 로그 서버 2 사용
            // 로그 서버 224.0.0.10:12900
            // 메세지 형식 본문 끝문자
            // 본문 메세지 수신 시간 :: 유저 엔드포인트 :: 메세지
            // 끝문자
            //UDP_RUN(12000,"18");

            //문제 2 Tcp서버 172.18.27.70:12100접속하고
            // 수신되는 메시지 정보를 확인
            // 172.18.27.xxx:????"[ETX] (끝문자 형식) 해당 서버로 추가 접속
            // 새로운 서버로 접속하여 메시지 전송
            // 학번 :: 영문이름 ETX
            // 재접속 시도는 1초에 한번 총 5회로 접속 불가시 메시지 띄우고 종료

            // 로그서버 172.18.27.70:12900 udp
            // 본문 끝
            // 수신 시간 :: 유저엔드포인트 :: 메시지
            // 끝문자 '\n'

            TCP_Run("172.18.27.70", 12123);

            //문제 3 tcp서버 22100동작
            // 다중접속 허용
            // 현재 접속자 정보 기록

        }

        private static void TCP_Run(string ipaddr, int port)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ipaddr), port);
            //IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 55555);

            Console.WriteLine("접속 시도");
            sock.Connect(remoteEP);
            Console.WriteLine("접속 성공");

            byte[] recvbuff = new byte[1024];
            //Thread.Sleep(5000);
            int retval = sock.Receive(recvbuff);




            Console.WriteLine("리시브가안되나");

            //127.0.0.1:44444[ETX]
            string recvstr = Encoding.UTF8.GetString(recvbuff, 0, retval);
            if (recvstr.Contains(EndStr2))
            {
                // 끝문자열 위치 확인
                int idx = recvstr.IndexOf(EndStr2);
                // 추출할 실제 데이터 공간 준비
                // 이미 string 객체로 변경한 temp 를 사용해서 substring으로 추출해도 됨.
                // 다른 형태의 데이터를 받았다고 가정하고 바이트 배열에서 복사하여 추출함.
                //byte[] data = new byte[buffer.Length];
                byte[] data = new byte[idx];
                Array.Copy(recvbuff, 0, data, 0, idx);
                string newipport = Encoding.UTF8.GetString(data);
                // 사용자 수신버퍼에서 data버퍼로 끝문자위치까지 복사
                Console.WriteLine($"[RECV-DATA] --> [{newipport}]");
                string newipport2 = newipport.Substring(1, 18);
                string[] words = newipport2.Split(':');
                string newip = null;
                string newport = null;
                for (int i = 0; i < 1; i++)
                {
                    newip = words[0];
                    newport = words[1];
                }
                Console.WriteLine(newip);
                Console.WriteLine(newport);
                int newportint = Convert.ToInt32(newport);
                newServer(newip, newportint);
            }



        }



        private static void newServer(string newip, int newportint)
        {
            Socket sock2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteEP2 = new IPEndPoint(IPAddress.Parse($"{newip}"), newportint);
            sock2.Connect(remoteEP2);

            string msg = "202227030::OhchanYoueng[ETX]";
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            int reta = sock2.Send(buffer, 0, buffer.Length, SocketFlags.None);
            Thread.Sleep(5000);
            sock2.Close();
            Console.WriteLine($"send byte{reta}, {Encoding.UTF8.GetString(buffer)}");

        }

        private static void SendLogServer(string ipaddr, int port, DateTime now, IPEndPoint recvEP, string sendMsg)
        {
            var remoteEP = new IPEndPoint(IPAddress.Loopback, 35000);
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //sock.Bind(remoteEP);
            var mcastOption = new MulticastOption(IPAddress.Parse(ipaddr), IPAddress.Any);
            //sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            string Logstr = $"{DateTime.Now}::{recvEP.Address}_{recvEP.Port}::{sendMsg}";
            byte[] Logbuffer = Encoding.UTF8.GetBytes(Logstr);
            var mEP = new IPEndPoint(IPAddress.Parse("224.0.0.10"), port);
            sock.SendTo(Logbuffer, mEP);
            Console.WriteLine(Logbuffer);

        }


        private static void UDP_RUN(int port, string n)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
            sock.Bind(remoteEP);

            IPEndPoint recvEP = new IPEndPoint(IPAddress.Parse("172.18.27." + n), 0);
            EndPoint recvRef = (EndPoint)recvEP;
            while (true)
            {
                byte[] buffer = new byte[1024];
                Console.WriteLine("리시브 대기");
                int retval = sock.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref recvRef);
                string recvdata = Encoding.UTF8.GetString(buffer, 0, retval);
                Console.WriteLine($"수신 - {recvdata}");

                byte[] Endbuf = Encoding.UTF8.GetBytes(EndStr);
                byte[] data = Encoding.UTF8.GetBytes(recvdata);
                byte[] sendbuf = new byte[data.Length + Endbuf.Length];

                Array.Copy(data, sendbuf, data.Length);
                Array.Copy(Endbuf, 0, sendbuf, data.Length, Endbuf.Length);
                string sendMsg = Encoding.UTF8.GetString(sendbuf);

                SendLogServer("224.0.0.10", 12900, DateTime.Now, recvEP, sendMsg);
            }
        }
    }
}
