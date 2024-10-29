using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Lean
{
    internal class Client
    {
        private static Thread sendThread;
        private static Thread recvThread;
        private static bool sendtoken;
        private static bool recvtoken;
        static void Main(string[] args)
        {
            // 예시 2
            // 172.18.27.70:30000 TCP 로 접속하여
            // 학번, 이름, 간단한 메세지 (< 100Bytes) 전송
            //SendSimple("127.0.0.1", 27000, "202227030, ohchanyoung, hi");

            // 2-1 접속하여 데이터 동시에 주고 받기  (Thread 반드시 사용해야만 한다)
            SendThread("127.0.0.1", 30000);
        }

        private static void SendThread(string v1, int v2)
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(v1), v2);
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            sock.Connect(serverEP);
            sendThread = new Thread(sendMsgThread);
            recvThread = new Thread(recvMsgThread);
            sendThread.Start(sock);
            recvThread.Start(sock);
            sendThread.Join();
            recvThread.Join();

            sock.Close();
            
        }

        private static void recvMsgThread(object? obj)
        {
            
            Socket sock = (Socket)obj;
            Socket C = sock.Accept();
            IPEndPoint cEP = (IPEndPoint)C.RemoteEndPoint;
            
            recvtoken = true;

            byte[] recvbuff = new byte[1500];
            int count = 0;
            try
            {
                while (recvtoken)
                {
                    count = C.Receive(recvbuff);
                    string recvStr = Encoding.UTF8.GetString(recvbuff);
                    if (count == 0)
                    {
                        sendtoken = false;
                        if(sock.Connected) sock.Disconnect(true);
                        break;
                    }
                    Console.WriteLine($"recv-{recvStr}");
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                recvThread.Interrupt();
            }
        }

        private static void sendMsgThread(object? obj)
        {
            Socket sock = (Socket)obj;
            sendtoken = true;
            int count = 0;
            try
            {
                while (sendtoken)
                {
                    while(Console.KeyAvailable) { Thread.Sleep(250); }
                    string Msg = Console.ReadLine();
                    byte[] buff = Encoding.UTF8.GetBytes(Msg);
                    if(Msg == "QUIT")
                    {
                        recvtoken = false;
                        if(sock.Connected) sock.Disconnect(true);
                        break;
                    }
                    count = sock.Send(buff, 0, buff.Length, SocketFlags.None);
                    Console.WriteLine($"Send: {Msg}");
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                sendThread.Interrupt();
            }

        }

        private static void SendSimple(string v1, int v2, string v3)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(v1), v2);
            sock.Connect(serverEP);
            byte[] buffer = Encoding.UTF8.GetBytes(v3);
            sock.Send(buffer, 0, buffer.Length, SocketFlags.None);
            sock.Close();
        }
    }
}