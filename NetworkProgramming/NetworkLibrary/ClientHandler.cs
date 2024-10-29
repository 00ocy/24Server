using Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace NetworkLibrary
{
    public class ClientHandler
    {
        private readonly ClientInfo _clientInfo;
        private FTP _ftpProtocol;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isRunning;
        private ConcurrentQueue<FTP> _packetQueue;

        public ClientHandler(ClientInfo clientInfo)
        {
            _clientInfo = clientInfo;
            _ftpProtocol = new FTP();
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
                        FTP requestProtocol = WaitForPacket();

                        if (requestProtocol == null)
                            break;

                        switch (requestProtocol.OpCode)
                        {
                            case OpCode.ConnectionRequest:
                                Console.WriteLine("클라이언트로부터 연결 요청을 받았습니다.");
                                FTP responseProtocol = new FTP();
                                byte[] responsePacket = responseProtocol.StartConnectionResponse(true);
                                _stream.Write(responsePacket, 0, responsePacket.Length);
                                break;

                            case OpCode.FileTransferRequest:
                                HandleFileTransferRequest(requestProtocol);
                                break;

                            case OpCode.FileListRequest:
                                HandleFileListRequest();
                                break;

                            case OpCode.FileDownloadRequest:
                                HandleFileDownloadRequest(requestProtocol);
                                break;

                            case OpCode.RequestTerminationAfterProcessing:
                                Console.WriteLine("클라이언트로부터 연결 종료 요청을 받았습니다.");
                                FTP disconnectResponse = new FTP();
                                byte[] disconnectPacket = disconnectResponse.DisconnectionResponse();
                                _stream.Write(disconnectPacket, 0, disconnectPacket.Length);
                                isConnected = false;
                                _isRunning = false;
                                break;
                            case OpCode.ReceptionCompletedSuccessfully:
                                Console.WriteLine($"수신 정상 완료");
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
                    packet.PrintPacketInfo("받은 패킷");
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
            byte[] headerBuffer = new byte[9];
            int bytesRead = _stream.Read(headerBuffer, 0, headerBuffer.Length);
            if (bytesRead == 0)
                throw new Exception("클라이언트가 연결을 종료하였습니다.");

            if (bytesRead < headerBuffer.Length)
                throw new Exception("패킷 헤더를 읽는 중 오류 발생");

            FTP protocol = FTP.ParsePacket(headerBuffer);

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
            string[] bodyParts = Encoding.UTF8.GetString(requestProtocol.Body).Split('\0');
            string filename = bodyParts[0];
            uint filesize = uint.Parse(bodyParts[1]);
            string clientFileHash = bodyParts[2];

            Console.WriteLine($"파일 전송 요청을 받았습니다. 파일명: {filename}, 크기: {filesize} bytes");

            FTP responseProtocol = new FTP();
            byte[] responsePacket = responseProtocol.TransmitFileResponse(true);
            responseProtocol.PrintPacketInfo("보낸 패킷");

            _stream.Write(responsePacket, 0, responsePacket.Length);

            ReceiveFileData(filename, filesize, clientFileHash);
        }

        private void HandleFileListRequest()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            currentDirectory = Path.Combine(currentDirectory, "S_UploadFiles");

            if (!Directory.Exists(currentDirectory))
            {
                Console.WriteLine($"디렉토리가 존재하지 않으므로 '{currentDirectory}'를 생성합니다.");
                Directory.CreateDirectory(currentDirectory);
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1kb.txt"), 1 * 1024);
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1mb.txt"), 1 * 1024 * 1024);
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_100mb.txt"), 100 * 1024 * 1024);
                DummyData.CreateDummyFile(Path.Combine(currentDirectory, "dummy_1gb.txt"), 1L * 1024 * 1024 * 1024);
                Console.WriteLine("더미 파일 생성이 완료되었습니다.");
            }

            string[] files = Directory.GetFiles(currentDirectory).Select(f => Path.GetFileName(f)).ToArray();
            FTP responseProtocol = new FTP();
            byte[] responsePacket = responseProtocol.GetFileListResponse(files);
            responseProtocol.PrintPacketInfo("보낸 패킷");

            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

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

                // 파일의 SHA-256 해시를 계산하고 바디에 포함하도록 응답 생성
                string fileHash = _ftpProtocol.CalculateFileHash(filePath);
                FTP responseProtocol = new FTP();

                // fileHash는 64자, 이를 UTF8 인코딩해 바이트 배열로 변환하고 추가
                byte[] responsePacket = responseProtocol.DownloadFileResponse(true, filesize, fileHash);
                responseProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(responsePacket, 0, responsePacket.Length);

                // 파일 데이터 전송
                SendFileData(filePath);
            }
            else
            {
                FTP responseProtocol = new FTP();
                byte[] responsePacket = responseProtocol.DownloadFileResponse(false);
                responseProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(responsePacket, 0, responsePacket.Length);
            }
        }

        private void SendFileData(string filePath)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            ushort seqNo = 0;

            string fileHash = _ftpProtocol.CalculateFileHash(filePath);
            Console.WriteLine($"\n[파일 전송 시작] 파일경로: {filePath}");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
                {
                    bool isFinal = fs.Position == fs.Length;
                    byte[] dataChunk = new byte[bytesRead];
                    Array.Copy(buffer, dataChunk, bytesRead);

                    byte[] dataPacket = _ftpProtocol.SendFileDataPacket(seqNo, dataChunk, isFinal);
                    _ftpProtocol.PrintPacketInfo("보낸 패킷");

                    _stream.Write(dataPacket, 0, dataPacket.Length);

                    seqNo++;
                }
            Console.WriteLine($"\n[파일 전송 끝] 파일경로: {filePath}");
            Console.WriteLine($"[SHA-256 해시] {fileHash}\n");
            }

        }

        private void ReceiveFileData(string filename, uint filesize, string expectedHash)
        {
            Dictionary<ushort, byte[]> fileChunks = new Dictionary<ushort, byte[]>();
            bool isReceiving = true;

            try
            {
                while (isReceiving && _isRunning)
                {
                    FTP dataPacket = WaitForPacket();

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

        private string SaveReceivedFile(string filename, Dictionary<ushort, byte[]> fileChunks)
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
    }
}
