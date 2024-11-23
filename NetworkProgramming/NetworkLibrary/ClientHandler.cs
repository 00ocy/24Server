using Protocol;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using SecurityLibrary;
namespace NetworkLibrary
{
    public class ClientHandler
    {
        private readonly ClientInfo _clientInfo;
        private FTP_Service _ftpService;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isRunning;
        private ConcurrentQueue<FTP> _packetQueue;

        public ClientHandler(ClientInfo clientInfo)
        {
            _clientInfo = clientInfo;
            _packetQueue = new ConcurrentQueue<FTP>();
            _isRunning = true;
        }

        public void HandleClient()
        {
            using (_stream = _clientInfo.Client.GetStream())
            {
                _ftpService = new FTP_Service(_stream, new FTP()); // FTP 인스턴스 전달

                // 람다 표현식 사용
                _receiveThread = new Thread(() => FTP_PacketListener.ReceivePackets(_stream, _packetQueue, _isRunning));
                _receiveThread.Start();

                try
                {
                    bool isConnected = true;

                    while (isConnected)
                    {
                        FTP requestProtocol = _ftpService.WaitForPacket(_packetQueue,_isRunning);

                        if (requestProtocol == null)
                            break;

                        HandleRequest(requestProtocol, ref isConnected);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"클라이언트 처리 중 오류 발생: {ex.Message}");
                }
                finally
                {
                    _isRunning = false;
                    _receiveThread.Join();
                    _clientInfo.Client.Close();
                }
            }
        }

        private void HandleRequest(FTP requestProtocol, ref bool isConnected)
        {
            switch (requestProtocol.OpCode)
            {
                case OpCode.ConnectionRequest: // 연결 요청
                    HandleConnectionRequest();
                    break;

                case OpCode.MessageModeRequest: // 메세지 모드 변경 요청
                    HandleChangeToMessageModeRequest();
                    break;

                case OpCode.MessageRequest: // 메세지 전송 요청
                    HandleMessageRequest(requestProtocol);
                    break;

                case OpCode.LoginRequest: // 로그인 요청
                    HandleLoginRequest(requestProtocol);
                    break;

                case OpCode.RegisterRequest: // 회원가입 요청
                    HandleRegisterRequest(requestProtocol);
                    break;

                case OpCode.DuplicateCheckRequest: // 아이디 중복 확인 요청
                    HandleDuplicateCheckRequest(requestProtocol);
                    break;
                    
                case OpCode.FileTransferRequest: // 파일 전송 요청
                    HandleFileTransferRequest(requestProtocol);
                    break;

                case OpCode.FileListRequest: // 파일 리스트 달라는 요청
                    HandleFileListRequest();
                    break;

                case OpCode.FileDownloadRequest: // 파일 보내달라 처리 요청
                    HandleFileDownloadRequest(requestProtocol);
                    break;

                case OpCode.RequestTerminationAfterProcessing: // 종료 할거라는 요청
                    HandleTerminationRequest(ref isConnected);
                    break;

                case OpCode.ReceptionCompletedSuccessfully: // 수신 잘 받았다는 요청
                    Console.WriteLine($"수신 정상 완료");
                    break;

                default:
                    Console.WriteLine($"알 수 없는 OpCode를 수신하였습니다: {requestProtocol.OpCode}");
                    break;
            }
        }

        private void HandleChangeToMessageModeRequest()
        { 
            // 메세지 모드 변경 요청 처리
            Console.WriteLine("메세지 모드 변경 요청을 받았습니다.");
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());


            // 변경 되었을 경우
            byte[] responsePacket = responseProtocol.ChangeMessageModeResponse(true);
            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        // 메시지 요청 처리
        private void HandleMessageRequest(FTP requestProtocol)
        {
            string message = Encoding.UTF8.GetString(requestProtocol.Body);
            Console.WriteLine($"{_clientInfo.Id}: {message}");

            // 수신 메시지 카운트 증가
            _clientInfo.IncreaseReciveMessagCount();
            _clientInfo.LastSendMessageTime = DateTime.Now;

            Logger.LogMessage(message, _clientInfo); // 메시지 로그 기록
        }

        // 회원가입 요청 처리
        private void HandleRegisterRequest(FTP requestProtocol)
        {
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());

            bool registerResult = Auth.Register(requestProtocol.Body);

            byte[] responsePacket = responseProtocol.RegisterResoponse(registerResult, OpCode.RegisterFailed);
            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        // 아이디 중복 확인 요청 처리
        private void HandleDuplicateCheckRequest(FTP requestProtocol)
        {
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());

            bool duplicaterResult = Auth.DuplicateCheck(requestProtocol.Body);

            byte[] responsePacket = responseProtocol.DuplicateCheckResoponse(duplicaterResult, OpCode.DuplicateCheckFailed);
            _stream.Write(responsePacket, 0, responsePacket.Length);

        }

        // 로그인 요청 처리
        private void HandleLoginRequest(FTP requestProtocol)
        {
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());

            bool loginResult = Auth.LoginCheck(requestProtocol.Body);
            byte[] responsePacket = responseProtocol.LoginResoponse(loginResult, OpCode.LoginFailed_ID);
            // ID 추출
            if(loginResult)
            {
                // 바디를 문자열로 변환 후 ':'로 분리
                string bodyString = Encoding.UTF8.GetString(requestProtocol.Body);
                string[] parts = bodyString.Split(':');

                _clientInfo.Id = parts[0]; // ID
                Console.WriteLine($"{_clientInfo.Id}님이 로그인 하셨습니다.");
                Logger.LogLogin(_clientInfo.Id, _clientInfo); // 로그인 로그 기록
            }
            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        // 접속 요청 처리
        private void HandleConnectionRequest() 
        {
            Console.WriteLine("클라이언트로부터 연결 요청을 받았습니다.");
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());
            byte[] responsePacket = responseProtocol.StartConnectionResponse(true);
            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        // 연결 요청 처리
        private void HandleTerminationRequest(ref bool isConnected)
        {
            Console.WriteLine("클라이언트로부터 연결 종료 요청을 받았습니다.");
            FTP_ResponsePacket disconnectResponse = new FTP_ResponsePacket(new FTP());
            byte[] disconnectPacket = disconnectResponse.DisconnectionResponse();
            _stream.Write(disconnectPacket, 0, disconnectPacket.Length);
            isConnected = false;
            _isRunning = false;
        }

        // 파일 전송 요청 처리
        private void HandleFileTransferRequest(FTP requestProtocol)
        {
            string[] bodyParts = Encoding.UTF8.GetString(requestProtocol.Body).Split('\0');
            string filename = bodyParts[0];
            uint filesize = uint.Parse(bodyParts[1]);
            string clientFileHash = bodyParts[2];

            Console.WriteLine($"파일 전송 요청을 받았습니다. 파일명: {filename}, 크기: {filesize} bytes");
            if(true)
            {
                // 파일명, 크기 제한 조건문 필요시 작성
            }
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());
            byte[] responsePacket = responseProtocol.TransmitFileResponse(true);
            responseProtocol.PrintPacketInfo("보낸 패킷");

            _stream.Write(responsePacket, 0, responsePacket.Length);

            bool result = _ftpService.ReceiveFileData(filename, filesize, clientFileHash, _packetQueue, _isRunning);
            if(result)
            {
                Logger.LogFileTransfer(filename, filesize, _clientInfo); // 파일 전송 로그 기록
            }
        }

        // 파일 목록 요청 처리
        private void HandleFileListRequest()
        {
            string currentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "S_UploadFiles");

            if (!Directory.Exists(currentDirectory))
            {
                Console.WriteLine($"디렉토리가 존재하지 않으므로 '{currentDirectory}'를 생성합니다.");
                Directory.CreateDirectory(currentDirectory);
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1kb.txt"), 1 * 1024);
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1mb.txt"), 1 * 1024 * 1024);
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_100mb.txt"), 100 * 1024 * 1024);
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1gb.txt"), 1L * 1024 * 1024 * 1024);
                Console.WriteLine("더미 파일 생성이 완료되었습니다.");
            }

            string[] files = Directory.GetFiles(currentDirectory).Select(Path.GetFileName).ToArray();
            FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());
            byte[] responsePacket = responseProtocol.GetFileListResponse(files);
            responseProtocol.PrintPacketInfo("보낸 패킷");

            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        // 파일 보내달라 요청 처리
        private void HandleFileDownloadRequest(FTP requestProtocol)
        {
            requestProtocol.PrintPacketInfo("받은 패킷");

            string filename = Encoding.UTF8.GetString(requestProtocol.Body);
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "S_UploadFiles");
            string filePath = Path.Combine(directoryPath, filename);

            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                uint filesize = (uint)fileInfo.Length;

                string fileHash = Hashing.CalculateFileHash(filePath);
                FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());

                byte[] responsePacket = responseProtocol.DownloadFileResponse(true, filesize, fileHash);
                responseProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(responsePacket, 0, responsePacket.Length);

                _ftpService.SendFileData(filePath);
            }
            else
            {
                FTP_ResponsePacket responseProtocol = new FTP_ResponsePacket(new FTP());
                byte[] responsePacket = responseProtocol.DownloadFileResponse(false);
                responseProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(responsePacket, 0, responsePacket.Length);
            }
        }
    }
}
