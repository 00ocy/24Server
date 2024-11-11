using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
namespace Protocol
{
    // 패킷 단위로 파일 전송 & 저장
    // 패킷 수신 대기 함수
    // 해시 계산 함수
    public class FTPManager
    {
        private readonly NetworkStream _stream;
        private readonly FTP _ftpProtocol;

        public FTPManager(NetworkStream stream, FTP ftpProtocol)
        {
            _stream = stream;
            _ftpProtocol = ftpProtocol;
        }

        // 파일을 읽고 데이터를 패킷 단위로 전송
        public void SendFileData(string filePath)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            uint seqNo = 0;

            string fileHash = CalculateFileHash(filePath);
            Console.WriteLine($"\n[파일 전송 시작] 파일경로: {filePath}");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
                {
                    bool isFinal = fs.Position == fs.Length;
                    byte[] dataChunk = new byte[bytesRead];
                    Array.Copy(buffer, dataChunk, bytesRead);

                    byte[] dataPacket = SendFileDataPacket(seqNo, dataChunk, isFinal);
                    _ftpProtocol.PrintPacketInfo("보낸 패킷");

                    _stream.Write(dataPacket, 0, dataPacket.Length);
                    seqNo++;
                }
            }

            Console.WriteLine($"\n[파일 전송 끝] 파일경로: {filePath}");
            Console.WriteLine($"[SHA-256 해시] {fileHash}\n");
        }
        // 파일 데이터 전송 패킷 생성 
        public byte[] SendFileDataPacket(uint seqNo, byte[] data, bool isFinal)
        {
            _ftpProtocol.OpCode = isFinal ? OpCode.FileDownloadDataEnd : OpCode.FileDownloadData;  // 파일 데이터 전송 여부에 따른 OpCode 설정
            _ftpProtocol.SeqNo = seqNo;  // 순차 번호 설정
            _ftpProtocol.Body = data;  // 전송할 데이터 설정
            _ftpProtocol.Length = (uint)(data != null ? data.Length : 0);  // 데이터 길이 설정
            return _ftpProtocol.GetPacket();  // 패킷 생성 및 반환
        }


        // 파일 데이터를 패킷 단위로 수신하고 파일로 저장
        public void ReceiveFileData(string filename, uint filesize, string expectedHash, ConcurrentQueue<FTP> packetQueue, bool isRunning)
        {
            Dictionary<uint, byte[]> fileChunks = new Dictionary<uint, byte[]>();
            bool isReceiving = true;

            try
            {
                while (isReceiving && isRunning)
                {
                    FTP dataPacket = WaitForPacket(packetQueue, isRunning, OpCode.FileDownloadData, OpCode.FileDownloadDataEnd);

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

                        string receivedFileHash = CalculateFileHash(receivedFilePath);
                        if (receivedFileHash == expectedHash)
                        {
                            Console.WriteLine($"\n[파일 수신 완료] 파일명: '{filename}'");
                            Console.WriteLine($"[SHA-256 해시] {receivedFileHash}\n");

                            FTP_ResponsePacket completionResponse = new FTP_ResponsePacket(new FTP());
                            byte[] completionPacket = completionResponse.TransferCompletionResponse(true);
                            _stream.Write(completionPacket, 0, completionPacket.Length);
                        }
                        else
                        {
                            //FileDownloadFailed_SHAhashValueDifferent
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
       

        // 파일의 해시 계산
        public string CalculateFileHash(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
                
        
        // 첫 번째 WaitForPacket 메서드 - 특정 조건 없이 패킷 대기
        public FTP WaitForPacket(ConcurrentQueue<FTP> packetQueue, bool isRunning)
        {
            while (isRunning)
            {
                if (packetQueue.TryDequeue(out FTP packet))
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
        public FTP WaitForPacket(ConcurrentQueue<FTP> packetQueue, bool isRunning, params OpCode[] expectedOpCodes)
        {
            int timeout = 5000;
            int waited = 0;
            while (isRunning && waited < timeout)
            {
                if (packetQueue.TryDequeue(out FTP packet))
                {
                    if (Array.Exists(expectedOpCodes, op => op == packet.OpCode))
                    {
                        return packet;
                    }
                }
                else
                {
                    Thread.Sleep(100);
                    waited += 100;
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
