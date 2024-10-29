using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NetworkLibrary
{
    public class ServerHandler
    {
        private readonly List<ClientInfo> _clientsList;
        private bool _isRunning;

        public ServerHandler()
        {
            _clientsList = new List<ClientInfo>();
        }

        // 클라이언트 추가
        public void AddClient(ClientInfo clientInfo)
        {            
            _clientsList.Add(clientInfo);
            Console.WriteLine($"New client connected: [{clientInfo.IpAddress}]:[{clientInfo.Port}]");
        }

        // 클라이언트 제거
        public void RemoveClient(TcpClient client)
        {
            var clientInfo = _clientsList.FirstOrDefault(c => c.Client == client);
            if (clientInfo != null)
            {
                clientInfo.Disconnect();
                Console.WriteLine($"Client disconnected: [{clientInfo.IpAddress}]:[{clientInfo.Port}]");
                _clientsList.Remove(clientInfo);
            }
        }

        // 연결된 모든 클라이언트의 연결을 종료 (서버 종료시)
        public void DisconnectAllClients()
        {
            BroadcastMessage("Sever Closed...");
            foreach (var client in _clientsList)
            {
                RemoveClient(client.Client);
            }
        }

        // 모든 클라이언트에게 메시지 보내기
        public void BroadcastMessage(string message)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            foreach (var clientInfo in _clientsList.Where(c => c.IsConnected))
            {
                using (NetworkStream stream = clientInfo.Client.GetStream())
                {
                    stream.Write(messageBuffer, 0, messageBuffer.Length);
                    Console.WriteLine($"Sent to [{clientInfo.IpAddress}]:[{clientInfo.Port}]: {message}");
                }
            }
        }
        // 특정 클라이언트에게 메시지 보내기
        public void SendMessageToOneClient(string message, ClientInfo clientInfo)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            using (NetworkStream stream = clientInfo.Client.GetStream())
            {
                stream.Write(messageBuffer, 0 , messageBuffer.Length);
                Console.WriteLine($"Success Send");
            }
        }
        // 총 접속자 수 확인
        public int GetConnectedClientCount()
        {
            return _clientsList.Count(c => c.IsConnected);
        }

        // 전체 접속자 정보 출력
        public void PrintAllClientInfo()
        {            
            Console.WriteLine("---- Active Clients ----");
            foreach (var client in _clientsList.Where(c => c.IsConnected))
            {
                Console.WriteLine($"[User.IP]: <{client.IpAddress}:{client.Port}> | [Recive, Send Count]: <{client.ReceiveMessageCount}, {client.SendMessageCount}> | [Last message time]: <{client.LastSendMessageTime.ToString("HH:mm:ss")}>");                
            }
        }


        public void StartCommandLoop(TcpServer server)
        {
            _isRunning = true;
            {
                while (_isRunning)
                {
                    string ?command = Console.ReadLine( );
                    if (command == "/info")
                    {
                        Console.WriteLine($"Connected Clients: {GetConnectedClientCount()}");
                        PrintAllClientInfo();
                    }
                    else if (command == "/exit")
                    {
                        Console.WriteLine("Closed Server...");
                        DisconnectAllClients();
                        _isRunning = false;
                        server.Stop();
                    }
                    else
                    {
                        //BroadcastMessage(command);
                    }
                }
            }
        }
    }
}
