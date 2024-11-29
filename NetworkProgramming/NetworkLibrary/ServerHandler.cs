using Protocol;
using System;
using System.Collections.Generic;
using System.IO;
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

                    // 전송 메시지 수 업데이트
                    clientInfo.IncreaseSendMessageCount();
                    clientInfo.LastSendMessageTime = DateTime.Now;

                    Console.WriteLine($"Sent to [{clientInfo.IpAddress}]:[{clientInfo.Port}]: {message}");
                }
            }
        }

        // 특정 클라이언트에게 메시지 보내기
        public void SendMessageToOneClient(string message, ClientInfo clientInfo)
        {
            try
            {
                // 메시지 송신
                FTP_RequestPacket messageRequest = new FTP_RequestPacket(new FTP());
                byte[] messagePacket = messageRequest.MessageSendRequest(message);

                // 메시지 전송
                NetworkStream stream = clientInfo.Client.GetStream();
                
                stream.Write(messagePacket, 0, messagePacket.Length);

                // 전송 카운트 업데이트
                clientInfo.IncreaseSendMessageCount();
                clientInfo.LastSendMessageTime = DateTime.Now;

                Logger.LogMessage("Server: "+message, clientInfo); // 메시지 로그 기록

                Console.WriteLine($"Message sent to {clientInfo.Id}: {message}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to {clientInfo.Id}: {ex.Message}");
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
                Console.WriteLine(
                    $"\n[User.ID]: <{client.Id ?? "Unknown"}> | " +
                    $"[User.IP]: <{client.IpAddress}:{client.Port}> | \n" +
                    $"[Receive, Send Count]: <{client.ReceiveMessageCount}, {client.SendMessageCount}> | " +
                    $"[Last message time]: <{client.LastSendMessageTime:HH:mm:ss}> | " +
                    $"[File Transfer]: <{(client.IsFileTransferInProgress ? "In Progress" : "Idle")}>"
                );
            }
        }


        public void StartCommandLoop(TcpServer server)
        {
            _isRunning = true;

            while (_isRunning)
            {
                string? command = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(command))
                    continue;

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
                else if (command.StartsWith("/send "))
                {
                    HandleSendCommand(command);
                }
                else
                {
                    Console.WriteLine("Invalid command. Available commands: /info, /exit, /send [ID] [Message]");
                }
            }
        }

        // 특정 클라이언트에게 메시지 보내기 명령 처리
        private void HandleSendCommand(string command)
        {
            try
            {
                // 명령어 파싱: "/send [ID] [Message]"
                string[] parts = command.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    Console.WriteLine("Invalid command format. Use: /send [ID] [Message]");
                    return;
                }

                string targetId = parts[1];
                string message = parts[2];

                // 대상 클라이언트 검색
                var targetClient = _clientsList.FirstOrDefault(c => c.Id == targetId && c.IsConnected);

                if (targetClient != null)
                {
                    SendMessageToOneClient(message, targetClient);
                    //Console.WriteLine($"Message sent to {targetId}: {message}1");
                }
                else
                {
                    Console.WriteLine($"Client with ID '{targetId}' not found or not connected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing /send command: {ex.Message}");
            }
        }
    }
}
