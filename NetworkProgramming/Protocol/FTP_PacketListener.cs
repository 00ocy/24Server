using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    public class FTP_PacketListener
    {
        // ReceivePackets 메서드 - 패킷 수신을 위한 작업 실행
        public static void ReceivePackets(NetworkStream stream ,ConcurrentQueue<FTP> packetQueue, bool isRunning)
        {
            try
            {
                while (isRunning)
                {
                    FTP packet = ReceivePacket(stream);

                    // 패킷 내용 출력
                    packet.PrintPacketInfo("받은 패킷");

                    // 패킷을 큐에 저장 (ConcurrentQueue는 스레드 안전함)
                    packetQueue.Enqueue(packet);
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
        public static FTP ReceivePacket(NetworkStream stream)
        {
            byte[] headerBuffer = new byte[11]; // 헤더 크기 (ProtoVer(1) + OpCode(2) + SeqNo(4) + Length(4))
            int bytesRead = stream.Read(headerBuffer, 0, headerBuffer.Length);
            if (bytesRead == 0)
                throw new Exception("패킷 헤더를 읽는 중 오류 발생");

            if (bytesRead < headerBuffer.Length)
                throw new Exception("패킷 헤더를 읽는 중 오류 발생");

            FTP protocol = ParsePacket(headerBuffer);

            // 바디가 있는 경우 바디 읽기
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
                protocol.Body = bodyBuffer;
            }

            return protocol;
        }
        // 수신된 데이터를 패킷으로 변환하는 메서드
        public static FTP ParsePacket(byte[] data)
        {
            FTP protocol = new FTP();  // 새 FTP 객체 생성
            using (MemoryStream ms = new MemoryStream(data))
            {
                protocol.ProtoVer = (byte)ms.ReadByte();  // 프로토콜 버전 읽기

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

                // Body 파싱 (Length가 0보다 클 경우에만)
                if (protocol.Length > 0)
                {
                    protocol.Body = new byte[protocol.Length];
                    ms.Read(protocol.Body, 0, (int)protocol.Length); // Body 데이터 읽기
                }
            }
            return protocol;  // 파싱된 FTP 패킷 반환
        }
    }

}
