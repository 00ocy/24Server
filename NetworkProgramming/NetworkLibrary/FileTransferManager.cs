using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Protocol;

public class FileTransferManager
{
    private readonly NetworkStream _stream;
    private readonly FTP _ftpProtocol;

    public FileTransferManager(NetworkStream stream, FTP ftpProtocol)
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

                byte[] dataPacket = _ftpProtocol.SendFileDataPacket(seqNo, dataChunk, isFinal);
                _ftpProtocol.PrintPacketInfo("보낸 패킷");

                _stream.Write(dataPacket, 0, dataPacket.Length);
                seqNo++;
            }
        }

        Console.WriteLine($"\n[파일 전송 끝] 파일경로: {filePath}");
        Console.WriteLine($"[SHA-256 해시] {fileHash}\n");
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

                        FTP completionResponse = new FTP();
                        byte[] responsePacket = completionResponse.TransferCompletionResponse(true);
                        _stream.Write(responsePacket, 0, responsePacket.Length);
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

    // 수신된 파일 저장
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

    // 패킷 수신 대기 (특정 OpCode에 맞는 패킷)
    private FTP WaitForPacket(ConcurrentQueue<FTP> packetQueue, bool isRunning, params OpCode[] expectedOpCodes)
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
}
