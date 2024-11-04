using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

public class FtpPacketHandler
{
    private readonly NetworkStream _stream;
    private readonly ConcurrentQueue<FTP> _packetQueue;
    private bool _isRunning;

    public FtpPacketHandler(NetworkStream stream, ConcurrentQueue<FTP> packetQueue, bool isRunning)
    {
        _stream = stream;
        _packetQueue = packetQueue;
        _isRunning = isRunning;
    }

    // 첫 번째 WaitForPacket 메서드 - 특정 조건 없이 패킷 대기
    public FTP WaitForPacket()
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

    // 두 번째 WaitForPacket 메서드 - 특정 OpCode에 맞는 패킷 대기
    public FTP WaitForPacket(params OpCode[] expectedOpCodes)
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

    // ReceivePackets 메서드 - 패킷 수신을 위한 작업 실행
    public void ReceivePackets()
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

    // ReceivePacket 메서드 - 단일 패킷을 스트림에서 읽기
    public FTP ReceivePacket()
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

}
