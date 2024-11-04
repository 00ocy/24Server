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
        private readonly ClientInfo _clientInfo;
        private FtpPacketHandler _packetHandler;
        private FileTransferManager _fileTransferManager;
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
                _packetHandler = new FtpPacketHandler(_stream, _packetQueue, _isRunning);
                _fileTransferManager = new FileTransferManager(_stream, new FTP()); // FTP 인스턴스 전달

                _receiveThread = new Thread(_packetHandler.ReceivePackets);
                _receiveThread.Start();

                try
                {
                    bool isConnected = true;

                    while (isConnected)
                    {
                        FTP requestProtocol = _packetHandler.WaitForPacket();

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
                case OpCode.ConnectionRequest:
                    HandleConnectionRequest();
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
                    HandleTerminationRequest(ref isConnected);
                    break;

                case OpCode.ReceptionCompletedSuccessfully:
                    Console.WriteLine($"수신 정상 완료");
                    break;

                default:
                    Console.WriteLine($"알 수 없는 OpCode를 수신하였습니다: {requestProtocol.OpCode}");
                    break;
            }
        }

        private void HandleConnectionRequest()
        {
            Console.WriteLine("클라이언트로부터 연결 요청을 받았습니다.");
            FTP responseProtocol = new FTP();
            byte[] responsePacket = responseProtocol.StartConnectionResponse(true);
            _stream.Write(responsePacket, 0, responsePacket.Length);
        }

        private void HandleTerminationRequest(ref bool isConnected)
        {
            Console.WriteLine("클라이언트로부터 연결 종료 요청을 받았습니다.");
            FTP disconnectResponse = new FTP();
            byte[] disconnectPacket = disconnectResponse.DisconnectionResponse();
            _stream.Write(disconnectPacket, 0, disconnectPacket.Length);
            isConnected = false;
            _isRunning = false;
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

            _fileTransferManager.ReceiveFileData(filename, filesize, clientFileHash, _packetQueue, _isRunning);
        }

        private void HandleFileListRequest()
        {
            string currentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "S_UploadFiles");

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

            string[] files = Directory.GetFiles(currentDirectory).Select(Path.GetFileName).ToArray();
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

                string fileHash = _fileTransferManager.CalculateFileHash(filePath);
                FTP responseProtocol = new FTP();

                byte[] responsePacket = responseProtocol.DownloadFileResponse(true, filesize, fileHash);
                responseProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(responsePacket, 0, responsePacket.Length);

                _fileTransferManager.SendFileData(filePath);
            }
            else
            {
                FTP responseProtocol = new FTP();
                byte[] responsePacket = responseProtocol.DownloadFileResponse(false);
                responseProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(responsePacket, 0, responsePacket.Length);
            }
        }
    }
}
