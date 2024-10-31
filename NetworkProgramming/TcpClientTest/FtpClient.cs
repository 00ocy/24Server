using Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TcpClientTest
{
    public class FtpClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private FTP _ftpProtocol;
        private IPEndPoint _remoteEp;
        private Thread _receiveThread;
        private bool _isRunning;
        private ConcurrentQueue<FTP> _packetQueue;

        public FtpClient(string serverIp, int serverPort)
        {
            _client = new TcpClient();
            _remoteEp = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _ftpProtocol = new FTP();
            _packetQueue = new ConcurrentQueue<FTP>();
        }

        public void ConnectServer()
        {
            try
            {
                Console.WriteLine("Connecting to Server...");
                _client.Connect(_remoteEp);
                _stream = _client.GetStream();

                Console.WriteLine("Connected!");
                Console.WriteLine($"Remote-[{_remoteEp.Address}]:[{_remoteEp.Port}]");
                Console.WriteLine("-------------------------------------------");

                // 수신 쓰레드 시작
                _isRunning = true;
                _receiveThread = new Thread(ReceivePackets);
                _receiveThread.Start();

                // 연결 요청 및 응답 처리
                if (!InitiateConnection())
                {
                    Console.WriteLine("서버와의 연결에 실패하였습니다.");
                    return;
                }

                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("Select File Transfer Mode");
                    Console.WriteLine("1. File Upload");
                    Console.WriteLine("2. File Download");
                    Console.WriteLine("3. Exit");

                    int select = 0;
                    bool success = int.TryParse(Console.ReadLine(), out select);

                    if (success)
                    {
                        switch (select)
                        {
                            case 1:
                                ShowClientUploadFiles();
                                break;
                            case 2:
                                ShowServerDownloadFiles();
                                break;
                            case 3:
                                exit = true;
                                break;
                            default:
                                Console.WriteLine("Invalid selection. Please try again.");
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error. Please enter a valid number.");
                    }
                }

                // 연결 종료 요청
                DisconnectServer();
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

        private bool InitiateConnection()
        {
            try
            {
                // 연결 요청 패킷 전송
                byte[] connectionRequest = _ftpProtocol.StartConnectionRequest();

                // 패킷 내용 출력
                _ftpProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(connectionRequest, 0, connectionRequest.Length);

                // 서버로부터 연결 응답 수신 대기
                FTP responseProtocol = WaitForPacket(OpCode.ConnectionOK, OpCode.ConnectionReject);

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
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1kb.txt"), 1 * 1024);          // 1KB
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1mb.txt"), 1 * 1024 * 1024);    // 1MB
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_100mb.txt"), 100 * 1024 * 1024); // 100MB
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1gb.txt"), 1L * 1024 * 1024 * 1024); // 1GB

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

                    SendFile(filesDict[select]);
                    break;
                }
                else
                {
                    Console.WriteLine("Error. Retry Please.");
                }
            }
        }

        private void ShowServerDownloadFiles()
        {
            // 서버에 파일 목록 요청
            byte[] fileListRequest = _ftpProtocol.GetFileListRequest();

            // 패킷 내용 출력
            _ftpProtocol.PrintPacketInfo("보낸 패킷");

            _stream.Write(fileListRequest, 0, fileListRequest.Length);

            // 서버로부터 파일 목록 응답 수신 대기
            FTP responseProtocol = WaitForPacket(OpCode.FileListResponse);

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
                // 파일 다운로드 요청 패킷 전송
                byte[] downloadRequest = _ftpProtocol.DownloadFileRequest(filename);
                _ftpProtocol.PrintPacketInfo("보낸 패킷");
                _stream.Write(downloadRequest, 0, downloadRequest.Length);

                // 서버로부터 다운로드 응답 수신 대기
                FTP responseProtocol = WaitForPacket(OpCode.FileDownloadOK, OpCode.FileDownloadFailed_FileNotFound);

                if (responseProtocol != null && responseProtocol.OpCode == OpCode.FileDownloadOK)
                {
                    Console.WriteLine("서버가 파일 다운로드 요청을 승인하였습니다.");

                    // 파일 크기 및 해시 수신
                    uint filesize = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(responseProtocol.Body, 0));
                    string serverFileHash = Encoding.UTF8.GetString(responseProtocol.Body, 4, 64); // 서버가 전송한 해시값 추출

                    // 파일 데이터 수신 및 해시 검증
                    ReceiveFileData(filename, filesize, serverFileHash);
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

        public void SendFile(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string filename = fileInfo.Name;
                uint filesize = (uint)fileInfo.Length;

                // 1. SHA-256 해시값 생성
                string fileHash = _ftpProtocol.CalculateFileHash(filePath);
                byte[] fileTransferRequest = _ftpProtocol.TransmitFileRequest(filename, filesize, fileHash);

                // 패킷 내용 출력
                _ftpProtocol.PrintPacketInfo("보낸 패킷");

                // 2. 파일 전송 요청 패킷 전송
                _stream.Write(fileTransferRequest, 0, fileTransferRequest.Length);

                // 3. 서버로부터 파일 전송 응답 수신 대기
                FTP responseProtocol = WaitForPacket(OpCode.FileTransferOK, OpCode.FileTransferFailed_FileName, OpCode.FileTransferFailed_FileSize);

                if (responseProtocol != null && responseProtocol.OpCode == OpCode.FileTransferOK)
                {
                    Console.WriteLine("서버가 파일 전송 요청을 승인하였습니다.");

                    // 4. 파일 데이터 전송
                    SendFileData(filePath);

                    // 5. 서버로부터 전송 완료 응답 수신 대기 및 해시 검증
                    responseProtocol = WaitForPacket(OpCode.ReceptionCompletedSuccessfully, OpCode.ReceptionFailedAndConnectionClosed);
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

        private void SendFileData(string filePath)
        {
            const int bufferSize = 4096; // 한 번에 읽을 바이트 수 (4KB)
            byte[] buffer = new byte[bufferSize];
            uint seqNo = 0; // 시퀀스 번호 초기화

            // 전송할 파일의 SHA-256 해시값 계산 및 출력
            string fileHash = _ftpProtocol.CalculateFileHash(filePath);
            Console.WriteLine($"\n[파일 전송 시작] 파일경로: {filePath}");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
                {
                    bool isFinal = fs.Position == fs.Length; // 파일의 마지막 청크인지 확인
                    byte[] dataChunk = new byte[bytesRead];
                    Array.Copy(buffer, dataChunk, bytesRead);

                    // FTP 클래스의 SendFileDataPacket 메서드를 사용하여 패킷 생성
                    byte[] dataPacket = _ftpProtocol.SendFileDataPacket(seqNo, dataChunk, isFinal);

                    // 패킷 내용 출력
                    _ftpProtocol.PrintPacketInfo("보낸 패킷");

                    // 데이터 패킷 전송
                    _stream.Write(dataPacket, 0, dataPacket.Length);

                    seqNo++;
                }
                Console.WriteLine($"\n[파일 전송 끝] 파일경로: {filePath}");
                Console.WriteLine($"[SHA-256 해시] {fileHash}\n");
            }
        }



        private void ReceiveFileData(string filename, uint filesize, string expectedHash)
        {
            Dictionary<uint, byte[]> fileChunks = new Dictionary<uint, byte[]>();
            bool isReceiving = true;

            try
            {
                while (isReceiving && _isRunning)
                {
                    FTP dataPacket = WaitForPacket(OpCode.FileDownloadData, OpCode.FileDownloadDataEnd);

                    if (dataPacket == null)
                    {
                        Console.WriteLine("파일 데이터 수신 타임아웃 또는 연결 종료.");
                        break;
                    }

                    fileChunks[dataPacket.SeqNo] = dataPacket.Body;

                    if (dataPacket.OpCode == OpCode.FileDownloadDataEnd)
                    {
                        isReceiving = false;
                        string receivedFilePath = SaveReceivedFile(filename, fileChunks);

                        // 수신된 파일 해시 검증
                        string receivedFileHash = _ftpProtocol.CalculateFileHash(receivedFilePath);
                        if (receivedFileHash == expectedHash)
                        {
                            Console.WriteLine($"\n[파일 수신 완료] 파일명: '{filename}'");
                            Console.WriteLine($"[SHA-256 해시] {receivedFileHash}\n");
                            // 전송 완료 응답 전송
                            FTP completionResponse = new FTP();
                            byte[] responsePacket = completionResponse.TransferCompletionResponse(true);
                            _stream.Write(responsePacket, 0, responsePacket.Length);

                        }
                        else
                        {
                            Console.WriteLine($"[받은 SHA-256 해시] {expectedHash}");
                            Console.WriteLine($"[현재 SHA-256 해시] {receivedFileHash}");
                            Console.WriteLine("파일 다운로드가 완료되었지만 해시값이 일치하지 않습니다. 파일이 손상되었을 수 있습니다.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 데이터 수신 중 오류 발생: {ex.Message}");
            }
        }



        private string SaveReceivedFile(string filename, Dictionary<uint, byte[]> fileChunks)
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DownloadFiles");
            Directory.CreateDirectory(directoryPath);
            string filePath = Path.Combine(directoryPath, filename);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var chunk in fileChunks.OrderBy(c => c.Key))
                {
                    fs.Write(chunk.Value, 0, chunk.Value.Length);
                }
            }

            return filePath;
        }

        private FTP WaitForPacket(params OpCode[] expectedOpCodes)
        {
            int timeout = 5000; // 5초 타임아웃
            int waited = 0;
            while (_isRunning && waited < timeout)
            {
                if (_packetQueue.TryDequeue(out FTP packet))
                {
                    if (Array.Exists(expectedOpCodes, op => op == packet.OpCode))
                    {
                        return packet;
                    }
                }
                else
                {
                    Thread.Sleep(100); // 100ms 대기
                    waited += 100;
                }
            }
            return null;
        }

        private void ReceivePackets()
        {
            try
            {
                while (_isRunning)
                {
                    FTP packet = ReceivePacket();

                    // 패킷 내용 출력
                    packet.PrintPacketInfo("받은 패킷");

                    // 패킷을 큐에 저장 (ConcurrentQueue는 스레드 안전함)
                    _packetQueue.Enqueue(packet);
                }
            }
            catch (IOException ioEx)
            {
                // 스트림이 닫히거나 네트워크 오류 발생 시
                Console.WriteLine($"패킷 수신 중 네트워크 오류 발생: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 수신 중 오류 발생: {ex.Message}");
            }
        }

        private FTP ReceivePacket()
        {
            byte[] headerBuffer = new byte[11]; // 헤더 크기 (ProtoVer(1) + OpCode(2) + SeqNo(4) + Length(4))
            int bytesRead = _stream.Read(headerBuffer, 0, headerBuffer.Length);
            if (bytesRead == 0)
                throw new Exception("패킷 헤더를 읽는 중 오류 발생");

            if (bytesRead < headerBuffer.Length)
                throw new Exception("패킷 헤더를 읽는 중 오류 발생");

            FTP protocol = FTP.ParsePacket(headerBuffer);

            // 바디가 있는 경우 바디 읽기
            if (protocol.Length > 0)
            {
                byte[] bodyBuffer = new byte[protocol.Length];
                int totalBytesRead = 0;
                while (totalBytesRead < protocol.Length)
                {
                    int read = _stream.Read(bodyBuffer, totalBytesRead, (int)(protocol.Length - totalBytesRead));
                    if (read <= 0)
                        throw new Exception("패킷 바디를 읽는 중 오류 발생");
                    totalBytesRead += read;
                }
                protocol.Body = bodyBuffer;
            }

            return protocol;
        }

        private void DisconnectServer()
        {
            try
            {
                // 연결 종료 요청 패킷 전송
                byte[] disconnectRequest = _ftpProtocol.DisconnectionRequest();

                // 패킷 내용 출력
                _ftpProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(disconnectRequest, 0, disconnectRequest.Length);

                // 서버로부터 연결 종료 응답 수신 대기
                FTP responseProtocol = WaitForPacket(OpCode.TerminationApprovedAndConnectionClosed);

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



    }
}
