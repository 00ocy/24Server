using SecurityLibrary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;


namespace Protocol
{
    public class FTP_PacketListener
    {
        // ReceivePackets 메서드 - 패킷 수신을 위한 작업 실행
        public static void ReceivePackets(NetworkStream stream, ConcurrentQueue<FTP> packetQueue, ConcurrentQueue<FTP> messageQueue, bool isRunning)
        {
            try
            {
                while (isRunning)
                {
                    FTP packet = ReceivePacket(stream);

                    // 메시지 모드 패킷은 별도 메시지 큐로 이동
                    if (packet.OpCode == OpCode.MessageRequest)
                    {
                        messageQueue.Enqueue(packet);
                    }
                    else
                    {
                        // 패킷 내용 출력
                        if (packet.OpCode != OpCode.SplitTransferInProgress && packet.OpCode != OpCode.SplitTransferFinal)
                        {
                            packet.PrintPacketInfo("받은 패킷");
                        }
                        packetQueue.Enqueue(packet);
                    }
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"패킷 수신 중 네트워크 오류 발생: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 수신 중 오류 발생: {ex.Message}");
            }
        }

        // ReceivePacket 메서드 - 단일 패킷을 스트림에서 읽기
        public static FTP ReceivePacket(NetworkStream stream)
        {
            byte[] headerBuffer = new byte[11];
            int bytesRead = stream.Read(headerBuffer, 0, headerBuffer.Length);
            if (bytesRead < headerBuffer.Length)
                throw new Exception("패킷 헤더를 읽는 중 오류 발생");

            FTP protocol = ParsePacket(headerBuffer);

            if (protocol.Length > 0)
            {
                byte[] bodyBuffer = new byte[protocol.Length];
                int totalBytesRead = 0;

                while (totalBytesRead < protocol.Length)
                {
                    int read = stream.Read(bodyBuffer, totalBytesRead, (int)(protocol.Length - totalBytesRead));
                    if (read <= 0)
                        throw new Exception("패킷 바디를 읽는 중 오류 발생");
                    totalBytesRead += read;
                }

                // Body 데이터를 복호화하여 설정
                protocol.Body = AESHelper.Decrypt(bodyBuffer);
            }

            return protocol;
        }

        // 수신된 데이터를 패킷으로 변환하는 메서드
        public static FTP ParsePacket(byte[] data)
        {
            FTP protocol = new FTP();
            using (MemoryStream ms = new MemoryStream(data))
            {
                protocol.ProtoVer = (byte)ms.ReadByte(); // 프로토콜 버전 읽기

                // OpCode 파싱
                byte[] buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                protocol.OpCode = (OpCode)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, 0));

                // SeqNo 파싱
                buffer = new byte[4];
                ms.Read(buffer, 0, 4);
                protocol.SeqNo = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));

                // Length 파싱
                buffer = new byte[4];
                ms.Read(buffer, 0, 4);
                protocol.Length = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
            }
            return protocol;
        }
    }
}
