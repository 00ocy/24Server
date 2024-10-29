using Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkLibrary
{
    public class ClientHandler
    {
        // 클라이언트 연결 객체
        private readonly ClientInfo _clientInfo;
        private FTP _ftpProtocol; // 파일 전송 프로토콜 객체
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isRunning;
        private ConcurrentQueue<FTP> _packetQueue;

        public ClientHandler(ClientInfo clientInfo)
        {
            _clientInfo = clientInfo;
            _ftpProtocol = new FTP();  // FTP 객체 초기화
            _packetQueue = new ConcurrentQueue<FTP>();
        }

        public void HandleClient()
        {
            using (_stream = _clientInfo.Client.GetStream())
            {
                _isRunning = true;
                _receiveThread = new Thread(ReceivePackets);
                _receiveThread.Start();

                try
                {
                    bool isConnected = true;

                    while (isConnected)
                    {
                        // 패킷 기달리기
                        FTP requestProtocol = WaitForPacket();

                        if (requestProtocol == null)
                            break;

                        switch (requestProtocol.OpCode)
                        {
                            case OpCode.ConnectionRequest:
                                Console.WriteLine("클라이언트로부터 연결 요청을 받았습니다.");

                                // 연결 승인 응답 전송
                                FTP responseProtocol = new FTP();
                                byte[] responsePacket = responseProtocol.StartConnectionResponse(true);
                                _stream.Write(responsePacket, 0, responsePacket.Length);

                                break;

                            case OpCode.FileTransferRequest:
                                // 파일 전송 요청 처리
                                HandleFileTransferRequest(requestProtocol);
                                break;

                            case OpCode.FileListRequest:
                                // 파일 목록 요청 처리
                                HandleFileListRequest();
                                break;

                            case OpCode.FileDownloadRequest:
                                // 파일 다운로드 요청 처리
                                HandleFileDownloadRequest(requestProtocol);
                                break;

                            case OpCode.RequestTerminationAfterProcessing:
                                Console.WriteLine("클라이언트로부터 연결 종료 요청을 받았습니다.");

                                // 연결 종료 승인 응답 전송
                                FTP disconnectResponse = new FTP();
                                byte[] disconnectPacket = disconnectResponse.DisconnectionResponse();
                                _stream.Write(disconnectPacket, 0, disconnectPacket.Length);

                                isConnected = false;
                                _isRunning = false;
                                break;

                            default:
                                Console.WriteLine($"알 수 없는 OpCode를 수신하였습니다: {requestProtocol.OpCode}");
                                break;
                        }
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

        private FTP WaitForPacket()
        {
            while (_isRunning)
            {
                if (_packetQueue.TryDequeue(out FTP packet))
                {
                    return packet;
                }
                else
                {
                    Thread.Sleep(10);
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
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 수신 중 오류 발생: {ex.Message}");
                _isRunning = false;
            }
        }

        private FTP ReceivePacket()
        {
            byte[] headerBuffer = new byte[9]; // 헤더 크기
            int bytesRead = _stream.Read(headerBuffer, 0, headerBuffer.Length);
            if (bytesRead == 0)
                throw new Exception("클라이언트가 연결을 종료하였습니다.");

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

        private void HandleFileTransferRequest(FTP requestProtocol)
        {
            // 파일 전송 요청 처리
            string[] bodyParts = Encoding.UTF8.GetString(requestProtocol.Body).Split('\0');
            string filename = bodyParts[0];
            uint filesize = uint.Parse(bodyParts[1]);

            Console.WriteLine($"파일 전송 요청을 받았습니다. 파일명: {filename}, 크기: {filesize} bytes");

            // 파일 전송 승인 응답 전송
            FTP responseProtocol = new FTP();
            byte[] responsePacket = responseProtocol.TransmitFileResponse(true);

            // 패킷 내용 출력
            responseProtocol.PrintPacketInfo("보낸 패킷");

            _stream.Write(responsePacket, 0, responsePacket.Length);

            // 파일 데이터 수신
            ReceiveFileData(filename, filesize);
        }

        private void HandleFileListRequest()
        {
            // 현재 디렉토리 경로 설정
            string currentDirectory = Directory.GetCurrentDirectory();
            currentDirectory = Path.Combine(currentDirectory, "S_UploadFiles");

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
            int index = 0;

            string[] files = Directory.GetFiles(currentDirectory)
                                 .Select(f => Path.GetFileName(f))
                                 .ToArray();

            // 파일 목록 응답 패킷 생성
            FTP responseProtocol = new FTP();
            byte[] responsePacket = responseProtocol.GetFileListResponse(files);

            // 패킷 내용 출력
            responseProtocol.PrintPacketInfo("보낸 패킷");

            // 클라이언트에게 파일 목록 전송
            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        private void HandleFileDownloadRequest(FTP requestProtocol)
        {
            // 패킷 내용 출력
            requestProtocol.PrintPacketInfo("받은 패킷");

            // 요청한 파일명 가져오기
            string filename = Encoding.UTF8.GetString(requestProtocol.Body);

            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "S_UploadFiles");
            string filePath = Path.Combine(directoryPath, filename);

            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                uint filesize = (uint)fileInfo.Length;

                // 파일 다운로드 승인 응답 전송 (파일 크기 포함)
                FTP responseProtocol = new FTP();
                byte[] responsePacket = responseProtocol.DownloadFileResponse(true, filesize);
                responseProtocol.PrintPacketInfo("보낸 패킷");
                _stream.Write(responsePacket, 0, responsePacket.Length);

                // 파일 데이터 전송
                SendFileData(filePath);
            }
            else
            {
                // 파일이 없을 경우 다운로드 실패 응답 전송
                FTP responseProtocol = new FTP();
                byte[] responsePacket = responseProtocol.DownloadFileResponse(false);
                responseProtocol.PrintPacketInfo("보낸 패킷");
                _stream.Write(responsePacket, 0, responsePacket.Length);
            }
        }
        private void SendFileData(string filePath)
        {
            const int bufferSize = 4096; // 한 번에 읽을 바이트 수 (4KB)
            byte[] buffer = new byte[bufferSize];
            ushort seqNo = 0; // 시퀀스 번호 초기화

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
            }
        }


        private void ReceiveFileData(string filename, uint filesize)
        {
            Dictionary<ushort, byte[]> fileChunks = new Dictionary<ushort, byte[]>();
            bool isReceiving = true;

            while (isReceiving && _isRunning)
            {
                FTP dataPacket = WaitForPacket();

                if (dataPacket == null)
                    break;

                if (dataPacket.OpCode == OpCode.SplitTransferInProgress || dataPacket.OpCode == OpCode.SplitTransferFinal)
                {
                    // 수신한 데이터 저장
                    fileChunks[dataPacket.SeqNo] = dataPacket.Body;

                    if (dataPacket.OpCode == OpCode.SplitTransferFinal)
                    {
                        // 모든 데이터 수신 완료
                        isReceiving = false;

                        // 파일 저장
                        SaveReceivedFile(filename, fileChunks);

                        // 전송 완료 응답 전송
                        FTP completionResponse = new FTP();
                        byte[] responsePacket = completionResponse.TransferCompletionResponse(true);
                        _stream.Write(responsePacket, 0, responsePacket.Length);
                    }
                }
                else
                {
                    // 예상치 못한 패킷 처리
                    Console.WriteLine("잘못된 패킷을 수신하였습니다.");
                }
            }
        }

        // 수신한 파일 저장 메서드
        private void SaveReceivedFile(string filename, Dictionary<ushort, byte[]> fileChunks)
        {
            string filePath = Path.Combine("S_DownloadFiles", filename);
            Directory.CreateDirectory("S_DownloadFiles");

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var chunk in fileChunks.OrderBy(c => c.Key))
                {
                    fs.Write(chunk.Value, 0, chunk.Value.Length);
                }
            }
            Console.WriteLine($"파일이 성공적으로 저장되었습니다: {filePath}");
        }
    }
}
