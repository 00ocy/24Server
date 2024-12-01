using System.Collections.Concurrent;
using System.Net.Sockets;
using SecurityLibrary;

namespace Protocol
{
    // 패킷 단위로 파일 전송 & 저장
    // 패킷 수신 대기 함수
    // 해시 계산 함수
    public class FTP_Service
    {
        private readonly NetworkStream _stream;
        private readonly FTP _ftpProtocol;

        public FTP_Service(NetworkStream stream, FTP ftpProtocol)
        {
            _stream = stream;
            _ftpProtocol = ftpProtocol;
        }

        public void SendFileData(string filePath)
        {
            const int bufferSize = 4096; // 한 번에 읽을 데이터 크기
            byte[] buffer = new byte[bufferSize];
            uint seqNo = 0;

            string fileHash = Hashing.CalculateFileHash(filePath);
            Console.WriteLine($"\n[파일 전송 시작] 파일경로: {filePath}\n");

            // 파일 크기 계산
            FileInfo fileInfo = new FileInfo(filePath);
            long totalFileSize = fileInfo.Length;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                long totalBytesSent = 0;

                // 진행 상태 출력 (프로그레스 바)
                Console.WriteLine("[파일 전송 진행 상태]");

                while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
                {
                    bool isFinal = fs.Position == fs.Length;
                    byte[] dataChunk = new byte[bytesRead];
                    Array.Copy(buffer, dataChunk, bytesRead);

                    byte[] dataPacket = SendFileDataPacket(seqNo, dataChunk, isFinal);
                    

                    _stream.Write(dataPacket, 0, dataPacket.Length);

                    totalBytesSent += bytesRead;

                    // 진행률 계산
                    float percent = (float)totalBytesSent / totalFileSize * 100;
                    int barCount = (int)(percent / (100f / 20));  // 20개의 프로그레스바로 표시

                    // 프로그레스 바 출력
                    Console.SetCursorPosition(0, Console.CursorTop);  // 같은 줄에 계속 출력
                    Console.Write($"[{new string('=', barCount)}{new string(' ', 20 - barCount)}] {percent:0.00}% [보낸 패킷] OpCode: {_ftpProtocol.OpCode} ({(int)_ftpProtocol.OpCode}) / SeqNo: {_ftpProtocol.SeqNo} / Length: {_ftpProtocol.Length}");
                    seqNo++;  // 순차 번호 증가

                }
            }

            Console.WriteLine($"\n\n[파일 전송 끝] 파일경로: {filePath}");
            Console.WriteLine($"\n[SHA-256 해시] {fileHash}\n");
        }


        // 파일 데이터 전송 패킷 생성 
        public byte[] SendFileDataPacket(uint seqNo, byte[] data, bool isFinal)
        {
            _ftpProtocol.OpCode = isFinal ? OpCode.SplitTransferFinal : OpCode.SplitTransferInProgress;
            _ftpProtocol.SeqNo = seqNo;

            // Body 데이터를 암호화하여 설정
            _ftpProtocol.Body = AESHelper.Encrypt(data);
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;

            return _ftpProtocol.GetPacket();
        }


        // 파일 데이터를 패킷 단위로 수신하고 파일로 저장
        public bool ReceiveFileData(string filename, uint filesize, string expectedHash, ConcurrentQueue<FTP> packetQueue, bool isRunning)
        {
            Dictionary<uint, byte[]> fileChunks = new Dictionary<uint, byte[]>();
            bool isReceiving = true;
            long totalBytesReceived = 0;

            Console.WriteLine("\n[파일 수신 진행 상태]");

            try
            {
                while (isReceiving && isRunning)
                {
                    FTP dataPacket = WaitForPacket(packetQueue, isRunning, OpCode.SplitTransferInProgress, OpCode.SplitTransferFinal);

                    if (dataPacket == null)
                    {
                        Console.WriteLine("파일 데이터 수신 타임아웃 또는 연결 종료.");
                        return false;
                    }

                    // 이미 복호화된 데이터를 사용
                    fileChunks[dataPacket.SeqNo] = dataPacket.Body;
                    totalBytesReceived += dataPacket.Body.Length;

                    // 진행률 출력
                    float percent = (float)totalBytesReceived / filesize * 100;
                    int barCount = (int)(percent / (100f / 20));

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"[{new string('=', barCount)}{new string(' ', 20 - barCount)}] {percent:0.00}% [받은 패킷] OpCode: {dataPacket.OpCode} ({(int)dataPacket.OpCode}) / SeqNo: {dataPacket.SeqNo} / Length: {dataPacket.Body.Length}");

                    if (dataPacket.OpCode == OpCode.SplitTransferFinal)
                    {
                        isReceiving = false;
                        string receivedFilePath = SaveReceivedFile(filename, fileChunks);

                        string receivedFileHash = Hashing.CalculateFileHash(receivedFilePath);
                        if (receivedFileHash == expectedHash)
                        {
                            Console.WriteLine($"\n\n[파일 수신 완료] 파일명: '{filename}'");
                            Console.WriteLine($"\n[SHA-256 해시] {receivedFileHash}\n");

                            FTP_ResponsePacket completionResponse = new FTP_ResponsePacket(new FTP());
                            byte[] completionPacket = completionResponse.TransferCompletionResponse(true);
                            completionResponse.PrintPacketInfo("보낸 패킷");
                            _stream.Write(completionPacket, 0, completionPacket.Length);
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"[받은 SHA-256 해시] {expectedHash}");
                            Console.WriteLine($"[현재 SHA-256 해시] {receivedFileHash}");
                            Console.WriteLine("파일 다운로드가 완료되었지만 해시값이 일치하지 않습니다. 파일이 손상되었을 수 있습니다.");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 데이터 수신 중 오류 발생: {ex.Message}");
                return false;
            }
            return false;
        }


        // 수신된 파일 저장 함수
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
       

        
        // 첫 번째 WaitForPacket 메서드 - 특정 조건 없이 패킷 대기
        public FTP WaitForPacket(ConcurrentQueue<FTP> targetQueue, bool isRunning)
        {
            while (isRunning)
            {
                if (targetQueue.TryDequeue(out FTP packet))
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
        // 패킷 수신 대기 (특정 OpCode에 맞는 패킷)
        public FTP WaitForPacket(ConcurrentQueue<FTP> targetQueue, bool isRunning, params OpCode[] expectedOpCodes)
        {
            int timeout = 10000;
            int waited = 0;
            while (isRunning && waited < timeout)
            {
                if (targetQueue.TryDequeue(out FTP packet))
                {
                    if (Array.Exists(expectedOpCodes, op => op == packet.OpCode))
                    {
                        return packet;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                    waited += 10;
                }
            }
            return null;
         
        }

        // 더미 파일 생성 메서드
        public static void CreateDummyFile(string filePath, long sizeInBytes)
        {
            byte[] data = new byte[1024]; // 1KB 크기의 데이터 블록 생성

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                long remainingBytes = sizeInBytes;

                // 남은 크기만큼 파일에 데이터 쓰기
                while (remainingBytes > 0)
                {
                    int bytesToWrite = (int)Math.Min(data.Length, remainingBytes); // 한번에 쓸 수 있는 데이터 크기 계산
                    fs.Write(data, 0, bytesToWrite); // 파일에 데이터 쓰기
                    remainingBytes -= bytesToWrite;  // 남은 크기 줄이기
                }
            }

            Console.WriteLine($"{filePath} ({sizeInBytes / 1024 / 1024}MB) 파일이 생성되었습니다.");
        }


    }
}
