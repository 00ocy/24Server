using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkLibrary
{
    public class TcpServer
    {
        private TcpListener _listener;
        private IPEndPoint _serverEp;
        private bool _isRunning;
        private ServerHandler _serverHandler; // 서버 핸들러 (관리)

        public TcpServer(IPEndPoint serverEp)
        {
            _serverEp = serverEp;
            _listener = new TcpListener(serverEp);
            _serverHandler = new ServerHandler();
        }

        public void Start()
        {
            _isRunning = true;
            _listener.Start(); // 서버 시작
            Console.WriteLine("Server started...");
            Console.WriteLine($"[{_serverEp?.Address}]:[{_serverEp?.Port}]");
            Console.WriteLine("-------------------------------------------");

            // 서버를 관리하는 스레드 시작
            Thread severHandlerThread = new Thread(() => _serverHandler.StartCommandLoop(this));
            severHandlerThread.Start();

            // 클라이언트 연결을 처리할 새로운 스레드 시작
            Thread acceptClientsThread = new Thread(AcceptClients);
            acceptClientsThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop(); // 서버 중지
            Console.WriteLine("Server stopped.");
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();                    
                    ClientInfo clientInfo = new ClientInfo(client);                    
                    _serverHandler.AddClient(clientInfo); // 클라이언트 관리에 추가

                    // 클라이언트 처리를 위한 새로운 스레드 생성
                    Thread clientThread = new Thread(() =>
                    {
                        ClientHandler clientHandler = new ClientHandler(clientInfo);
                        clientHandler.HandleClient();
                        _serverHandler.RemoveClient(client);
                    });
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    if (!_isRunning) break;
                }
            }
        }
    }
}
