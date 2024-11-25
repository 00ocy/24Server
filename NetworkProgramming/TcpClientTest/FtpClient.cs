using Protocol;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using SecurityLibrary;
using NetworkLibrary;
namespace TcpClientTest
{
    public class FtpClient
    {
        private TcpClient _client;
        private FTP_Service _ftpService;
        private NetworkStream _stream;
        private IPEndPoint _remoteEp;
        private Thread _receiveThread;
        private bool _isRunning;
        private ConcurrentQueue<FTP> _packetQueue;
        private ConcurrentQueue<FTP> _messageQueue; // 메시지 전용 큐

        public FtpClient(string serverIp, int serverPort)
        {
            _client = new TcpClient();
            _remoteEp = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _packetQueue = new ConcurrentQueue<FTP>();
            _messageQueue = new ConcurrentQueue<FTP>();
        }

        public void ConnectServer()
        {
            try
            {
                Console.WriteLine("Connecting to Server...");
                _client.Connect(_remoteEp);
                _stream = _client.GetStream();
                _ftpService = new FTP_Service(_stream, new FTP());

                Console.WriteLine("Connected!");
                Console.WriteLine($"Remote-[{_remoteEp.Address}]:[{_remoteEp.Port}]");
                Console.WriteLine("-------------------------------------------");

                _isRunning = true;

                // 수신 쓰레드 시작
                _receiveThread = new Thread(() => FTP_PacketListener.ReceivePackets(_stream, _packetQueue, _messageQueue, _isRunning));
                _receiveThread.Start();
                _isRunning = true;

                // 메시지 수신 처리 비동기 루프 시작
                _ = Task.Run(() => ProcessMessagePacketsAsync());


                // 연결 요청 및 응답 처리
                if (!ConnectionCheck())
                {
                    Console.WriteLine("서버와의 연결에 실패하였습니다.");
                    return;
                }
                // 로그인 및 회원가입 처리
                bool authenticated = false;

                while (!authenticated)
                {
                    Console.WriteLine("1. 로그인");
                    Console.WriteLine("2. 회원가입");
                    Console.WriteLine("3. 종료");

                    int option;
                    if (int.TryParse(Console.ReadLine(), out option))
                    {
                        switch (option)
                        {
                            case 1:
                                authenticated = userLogin();
                                break;
                            case 2:
                                userRegister();
                                break;
                            case 3:
                                DisconnectServer();
                                return;
                            default:
                                Console.WriteLine("잘못된 선택입니다. 다시 시도해주세요.");
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("숫자를 입력해주세요.");
                    }
                }

                // ChooseAction 실행
                ChooseAction();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                try
                {
                    _receiveThread?.Join();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"스레드 종료 중 오류 발생: {ex.Message}");
                }
                _stream?.Close();
                _client?.Close();
            }
        }
        private async Task ProcessMessagePacketsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    if (_messageQueue.TryDequeue(out FTP messageProtocol))
                    {
                        if (messageProtocol.OpCode == OpCode.MessageRequest)
                        {
                            string message = Encoding.UTF8.GetString(messageProtocol.Body);
                            Console.WriteLine($"Server: {message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"메시지 처리 중 오류 발생: {ex.Message}");
                }

                await Task.Delay(10); // CPU 과부하 방지
            }
        }

        private bool userLogin()
        {
            Console.WriteLine("아이디를 입력하세요:");
            string userId = Console.ReadLine();

            Console.WriteLine("비밀번호를 입력하세요:");
            string password = Console.ReadLine();
            string encryptedPassword = AESHelper.Encrypt(password);

            // ID, PW를 ':'로 결합
            string encryptedIDPW = $"{userId}:{encryptedPassword}";

            // 패킷에 담아 보내기
            FTP_RequestPacket loginRequest = new FTP_RequestPacket(new FTP());
            byte[] loginnPacket = loginRequest.LoginRequest(encryptedIDPW);

            // 패킷 내용 출력
            loginRequest.PrintPacketInfo("보낸 패킷");

            _stream.Write(loginnPacket, 0, loginnPacket.Length);

            // 서버로부터 로그인 결과 수신 대기
            FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning);
            if (responseProtocol != null && responseProtocol.OpCode == OpCode.LoginOK)
            {
                Console.WriteLine("로그인 성공");
                return true;
            }
            else
            {
                Console.WriteLine($"로그인 실패");
                return false;
            }
        }

        private void userRegister()
        {
            Console.WriteLine("아이디를 입력하세요:");
            string userId = Console.ReadLine();
            //string encryptedId = AESHelper.Encrypt(userId);

            // id를 담은 패킷을 보내 서버에서 중복 확인
            if (!userDuplicateCheck(userId))
            {
                return;
            }

            // PW 생성 후 암호화
            Console.WriteLine("비밀번호를 입력하세요:");
            string password = Console.ReadLine();
            string encryptedPassword = AESHelper.Encrypt(password);

            // ID,PW를':'로 결합
            string encryptedIDPW = $"{userId}:{encryptedPassword}";

            // 패킷에 담아 전송
            FTP_RequestPacket registerRequest = new FTP_RequestPacket(new FTP());
            byte[] registerPacket = registerRequest.RegisterRequest(encryptedIDPW);
            _stream.Write(registerPacket, 0, registerPacket.Length);


            // 서버로부터 회원가입 결과 대기
            FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning);
            if (responseProtocol != null && responseProtocol.OpCode == OpCode.RegisterOK)
            {
                Console.WriteLine("회원가입 성공");
            }
            else
            {
                Console.WriteLine($"회원가입 실패");
            }
        }

        public bool userDuplicateCheck(string userId)
        {
            FTP_RequestPacket DuplicateCheckRequest = new FTP_RequestPacket(new FTP());
            byte[] DuplicateCheckPacket = DuplicateCheckRequest.DuplicateCheckRequest(userId);

            // 패킷 내용 출력
            DuplicateCheckRequest.PrintPacketInfo("보낸 패킷");

            _stream.Write(DuplicateCheckPacket, 0, DuplicateCheckPacket.Length);

            // 서버로부터 연결 응답 수신 대기
            FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning);
            if (responseProtocol != null && responseProtocol.OpCode == OpCode.DuplicateCheckOK)
            {
                Console.WriteLine("사용할 수 있는 아이디입니다.");
                return true;
            }
            else
            {
                Console.WriteLine($"사용할 수 없는 아이디입니다.");
                return false;
            }
        }


        private void ChooseAction()
        {
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("선택 해주세요");
                Console.WriteLine("1. 메세지 모드로 변경");
                Console.WriteLine("2. 파일 업로드");
                Console.WriteLine("3. 파일 다운로드");
                Console.WriteLine("4. 종료");

                int select = 0;
                bool success = int.TryParse(Console.ReadLine(), out select);

                if (success)
                {
                    switch (select)
                    {
                        case 1:
                            ChangeToMessageMode();
                            break;
                        case 2:
                            ShowClientUploadFiles();
                            break;
                        case 3:
                            ShowServerDownloadFiles();
                            break;
                        case 4:
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("잘못된 선택입니다.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("숫자를 입력해주세요.");
                }
            }
        }


        private bool ConnectionCheck()
        {
            try
            {
                FTP_RequestPacket connectionRequest = new FTP_RequestPacket(new FTP());

                // 연결 요청 패킷 전송
                byte[] connectionPacket = connectionRequest.StartConnectionRequest();

                // 패킷 내용 출력
                connectionRequest.PrintPacketInfo("보낸 패킷");

                _stream.Write(connectionPacket, 0, connectionPacket.Length);

                // 서버로부터 연결 응답 수신 대기
                FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning, OpCode.ConnectionOK, OpCode.ConnectionReject);

                if (responseProtocol != null && responseProtocol.OpCode == OpCode.ConnectionOK)
                {
                    Console.WriteLine("서버와의 연결이 성공적으로 수립되었습니다.");
                    return true;
                }
                else
                {
                    Console.WriteLine("서버와의 연결이 거부되었습니다.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        private void DisconnectServer()
        {
            try
            {
                FTP_RequestPacket disconnectRequest = new FTP_RequestPacket(new FTP());
                // 연결 종료 요청 패킷 전송
                byte[] disconnectPacket = disconnectRequest.DisconnectionRequest();

                // 패킷 내용 출력
                disconnectRequest.PrintPacketInfo("보낸 패킷");

                _stream.Write(disconnectPacket, 0, disconnectPacket.Length);

                // 서버로부터 연결 종료 응답 수신 대기
                FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning, OpCode.TerminationApprovedAndConnectionClosed);

                if (responseProtocol != null && responseProtocol.OpCode == OpCode.TerminationApprovedAndConnectionClosed)
                {
                    Console.WriteLine("서버가 연결 종료를 승인하였습니다.");
                }
                else
                {
                    Console.WriteLine("서버가 연결 종료 요청을 거부하였습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 종료 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // ReceivePackets 스레드를 종료하기 위해 _isRunning을 false로 설정
                _isRunning = false;

                // 스트림과 클라이언트를 닫아 ReceivePackets 스레드가 블록을 벗어나도록 함
                try
                {
                    _stream?.Close();
                    _client?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"스트림 또는 클라이언트 종료 중 오류 발생: {ex.Message}");
                }
            }
        }

        public void ChangeToMessageMode()
        {
            FTP_RequestPacket MessageModeRequest = new FTP_RequestPacket(new FTP());

            byte[] MessageModePacket = MessageModeRequest.ChangeToMessageModeRequest();

            // 패킷 내용 출력
            MessageModeRequest.PrintPacketInfo("보낸 패킷");


            _stream.Write(MessageModePacket, 0, MessageModePacket.Length);

            // 3. 서버로부터 파일 전송 응답 수신 대기
            FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning);
            if (responseProtocol != null && responseProtocol.OpCode == OpCode.MessageModeOK)
            {
                HandleMessageMode();
            }
        }
        public void HandleMessageMode()
        {
            Console.WriteLine("메시지 모드로 진입하였습니다. '/EXIT'를 입력하면 종료됩니다.");
            string userInput;

            while (true)
            {
                Console.Write("입력: ");
                userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine("빈 메시지는 전송할 수 없습니다.");
                    continue;
                }

                if (userInput.Equals("/EXIT", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("메시지 모드에서 나갑니다.");
                    break;
                }

                try
                {
                    // 메시지 송신
                    FTP_RequestPacket messageRequest = new FTP_RequestPacket(new FTP());
                    byte[] messagePacket = messageRequest.MessageSendRequest(userInput);

                    _stream.Write(messagePacket, 0, messagePacket.Length);
                    Console.WriteLine("메시지를 서버로 전송하였습니다.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"메시지 전송 중 오류 발생: {ex.Message}");
                }
            }
        }
        public void ShowClientUploadFiles()
        {
            // 현재 디렉토리 경로 설정
            string currentDirectory = Directory.GetCurrentDirectory();
            currentDirectory = Path.Combine(currentDirectory, "UploadFiles");

            // 디렉토리가 존재하지 않으면 생성
            if (!Directory.Exists(currentDirectory))
            {
                Console.WriteLine($"디렉토리가 존재하지 않으므로 '{currentDirectory}'를 생성합니다.");
                Directory.CreateDirectory(currentDirectory);

                // 더미 파일 생성                
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1kb.txt"), 1 * 1024);          // 1KB
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1mb.txt"), 1 * 1024 * 1024);    // 1MB
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_100mb.txt"), 100 * 1024 * 1024); // 100MB
                FTP_Service.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1gb.txt"), 1L * 1024 * 1024 * 1024); // 1GB

                Console.WriteLine("더미 파일 생성이 완료되었습니다.");
            }

            // 해당 디렉토리의 파일 목록 가져오기
            string[] files = Directory.GetFiles(currentDirectory);
            if (files.Length == 0)
            {
                Console.WriteLine("전송할 파일이 없습니다.");
                return;
            }

            int select = 0;
            bool success = false;
            Dictionary<int, string> filesDict = new Dictionary<int, string>();

            while (!success)
            {
                Console.WriteLine("Which file do you want to upload?");
                Console.WriteLine($"Directory: {currentDirectory}");
                int dirCount = 0;
                foreach (string file in files)
                {
                    dirCount++;
                    if (!filesDict.ContainsKey(dirCount))
                        filesDict.Add(dirCount, file);
                    Console.Write($"{dirCount}. ");
                    Console.WriteLine(Path.GetFileName(file)); // 파일 이름만 출력
                }
                Console.WriteLine($"{dirCount + 1}. Cancel");

                success = int.TryParse(Console.ReadLine(), out select) && (select >= 1 && select <= filesDict.Count + 1);
                if (success)
                {
                    if (select == dirCount + 1)
                    {
                        // 취소 선택
                        Console.WriteLine("파일 업로드를 취소하였습니다.");
                        return;
                    }

                    UploadFile(filesDict[select]);
                    break;
                }
                else
                {
                    Console.WriteLine("Error. Retry Please.");
                }
            }
        }

        public void UploadFile(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string filename = fileInfo.Name;
                uint filesize = (uint)fileInfo.Length;

                // 1. SHA-256 해시값 생성
                string fileHash = Hashing.CalculateFileHash(filePath);
                FTP_RequestPacket fileTransferRequest = new FTP_RequestPacket(new FTP());

                byte[] fileTransferPacket = fileTransferRequest.TransmitFileRequest(filename, filesize, fileHash);

                // 패킷 내용 출력
                fileTransferRequest.PrintPacketInfo("보낸 패킷");

                // 2. 파일 전송 요청 패킷 전송
                _stream.Write(fileTransferPacket, 0, fileTransferPacket.Length);

                // 3. 서버로부터 파일 전송 응답 수신 대기
                FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning, OpCode.FileTransferOK, OpCode.FileTransferFailed_FileName, OpCode.FileTransferFailed_FileSize);

                if (responseProtocol != null && responseProtocol.OpCode == OpCode.FileTransferOK)
                {
                    Console.WriteLine("서버가 파일 전송 요청을 승인하였습니다.");

                    // 4. 파일 데이터 전송
                    _ftpService.SendFileData(filePath);

                    // 5. 서버로부터 전송 완료 응답 수신 대기 및 해시 검증
                    responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning, OpCode.ReceptionCompletedSuccessfully, OpCode.ReceptionFailedAndConnectionClosed);
                    if (responseProtocol != null && responseProtocol.OpCode == OpCode.ReceptionCompletedSuccessfully)
                    {
                        // 서버가 파일 해시를 확인하고 전송이 성공했음을 알림
                        Console.WriteLine("파일 전송이 성공적으로 완료되었습니다.");
                    }
                    else
                    {
                        Console.WriteLine("파일 전송이 실패하였습니다.");
                    }
                }
                else
                {
                    Console.WriteLine("서버가 파일 전송 요청을 거부하였습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        private void ShowServerDownloadFiles()
        {
            FTP_RequestPacket fileListRequest = new FTP_RequestPacket(new FTP());

            // 서버에 파일 목록 요청
            byte[] fileListPacket = fileListRequest.GetFileListRequest();

            // 패킷 내용 출력
            fileListRequest.PrintPacketInfo("보낸 패킷");

            _stream.Write(fileListPacket, 0, fileListPacket.Length);

            // 서버로부터 파일 목록 응답 수신 대기
            FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning, OpCode.FileListResponse);

            if (responseProtocol != null && responseProtocol.OpCode == OpCode.FileListResponse)
            {
                // 파일 목록 수신
                string fileListString = Encoding.UTF8.GetString(responseProtocol.Body);
                string[] files = fileListString.Split('\0');

                if (files.Length == 0 || (files.Length == 1 && string.IsNullOrEmpty(files[0])))
                {
                    Console.WriteLine("서버에 다운로드 가능한 파일이 없습니다.");
                    return;
                }

                // 파일 목록 표시 및 다운로드할 파일 선택
                SelectAndDownloadFile(files);
            }
            else
            {
                Console.WriteLine("서버로부터 파일 목록을 받는 데 실패하였습니다.");
            }
        }

        private void SelectAndDownloadFile(string[] files)
        {
            int select = 0;
            bool success = false;
            Dictionary<int, string> filesDict = new Dictionary<int, string>();

            while (!success)
            {
                Console.WriteLine("Which file do you want to download?");
                int fileCount = 0;
                foreach (string file in files)
                {
                    fileCount++;
                    if (!filesDict.ContainsKey(fileCount))
                        filesDict.Add(fileCount, file);
                    Console.WriteLine($"{fileCount}. {file}");
                }
                Console.WriteLine($"{fileCount + 1}. Cancel");

                success = int.TryParse(Console.ReadLine(), out select) && (select >= 1 && select <= filesDict.Count + 1);
                if (success)
                {
                    if (select == fileCount + 1)
                    {
                        // 취소 선택
                        Console.WriteLine("파일 다운로드를 취소하였습니다.");
                        return;
                    }

                    // 선택한 파일 다운로드
                    DownloadFile(filesDict[select]);
                    break;
                }
                else
                {
                    Console.WriteLine("Error. Retry Please.");
                }
            }
        }

        public void DownloadFile(string filename)
        {
            try
            {
                FTP_RequestPacket downloadRequest = new FTP_RequestPacket(new FTP());
                // 파일 다운로드 요청 패킷 전송
                byte[] downloadPacket = downloadRequest.DownloadFileRequest(filename);
                downloadRequest.PrintPacketInfo("보낸 패킷");
                _stream.Write(downloadPacket, 0, downloadPacket.Length);

                // 서버로부터 다운로드 응답 수신 대기
                FTP responseProtocol = _ftpService.WaitForPacket(_packetQueue, _isRunning, OpCode.FileDownloadOK, OpCode.FileDownloadFailed_FileNotFound);

                if (responseProtocol != null && responseProtocol.OpCode == OpCode.FileDownloadOK)
                {
                    Console.WriteLine("서버가 파일 다운로드 요청을 승인하였습니다.");

                    // 파일 크기 및 해시 수신
                    uint filesize = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(responseProtocol.Body, 0));
                    string serverFileHash = Encoding.UTF8.GetString(responseProtocol.Body, 4, 64); // 서버가 전송한 해시값 추출

                    // 파일 데이터 수신 및 해시 검증
                    _ftpService.ReceiveFileData(filename, filesize, serverFileHash, _packetQueue, _isRunning);
                }
                else
                {
                    Console.WriteLine("서버가 파일 다운로드 요청을 거부하였습니다. 파일이 존재하지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }






    }
}
