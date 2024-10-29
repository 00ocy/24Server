// FTP 패킷을 처리하는 클래스
using System.Net;
using System.Text;

// FTP 패킷을 처리하는 클래스
public class FTP
{
    public byte ProtoVer { get; private set; }          // 프로토콜 버전
    public OpCode OpCode { get; private set; }          // 명령 코드 (OpCode enum 사용)
    public ushort SeqNo { get; private set; }           // 순차 번호 (데이터 분할 전송 시 사용)
    public uint Length { get; private set; }            // 데이터 길이 (본문의 길이)
    public byte[]? Body { get; set; }            // 데이터 본문 (null 허용)

    // FTP 객체 생성 시 초기화
    public FTP()
    {
        ProtoVer = 1;               // 기본 프로토콜 버전 1
        OpCode = OpCode.ConnectionOK; // 기본 명령 코드 (ConnectionOK)
        SeqNo = 0;                  // 기본 순차 번호 0
        Length = 0;                 // 데이터 길이 0
        Body = null;                // 본문은 없음
    }

    // 헤더 생성 메서드 (헤더에 필요한 정보들을 바이트 배열로 반환)
    private byte[] MakeHeader()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.WriteByte(ProtoVer); // 프로토콜 버전 추가
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)OpCode)), 0, 2); // OpCode 추가 (2바이트)
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)SeqNo)), 0, 2);  // SeqNo 추가 (2바이트)
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Length)), 0, 4);   // Length 추가 (4바이트)
            return ms.ToArray();  // 완성된 헤더 반환
        }
    }

    // 전체 패킷 생성 (헤더 + 바디를 결합)
    public byte[] GetPacket()
    {
        byte[] header = MakeHeader();  // 헤더 생성
        if (Body != null && Body.Length > 0)
        {
            byte[] packet = new byte[header.Length + Body.Length]; // 헤더와 바디를 포함하는 패킷 배열 생성
            Buffer.BlockCopy(header, 0, packet, 0, header.Length); // 헤더 복사
            Buffer.BlockCopy(Body, 0, packet, header.Length, Body.Length); // 바디 복사
            return packet; // 최종 패킷 반환
        }
        else
        {
            return header; // 바디가 없으면 헤더만 반환
        }
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
            ms.Read(buffer, 0, 2);
            protocol.SeqNo = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, 0));

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

    // 연결 요청 패킷 생성
    public byte[] StartConnectionRequest()
    {
        OpCode = OpCode.ConnectionRequest; // 접속 요청 코드 (000)
        Length = 0; // 바디가 없으므로 길이는 0
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 연결 응답 패킷 생성
    public byte[] StartConnectionResponse(bool ok)
    {
        OpCode = ok ? OpCode.ConnectionOK : OpCode.ConnectionReject; // OK(001), REJECT(002) 중 하나 선택
        Length = 0;
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 파일 전송 요청 패킷 생성
    public byte[] TransmitFileRequest(string filename, uint filesize)
    {
        OpCode = OpCode.FileTransferRequest;  // 파일 전송 요청 코드 (100)
        Body = Encoding.UTF8.GetBytes(filename + "\0" + filesize.ToString());  // 파일명과 파일 크기를 바디에 저장
        Length = (uint)Body.Length;  // 바디의 길이 저장
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 파일 전송 응답 패킷 생성
    public byte[] TransmitFileResponse(bool ok, OpCode errorCode = OpCode.FileTransferOK)
    {
        OpCode = ok ? OpCode.FileTransferOK : errorCode;  // 성공 여부에 따라 OpCode 설정
        Length = 0;
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 데이터 패킷 생성 (파일 전송 중 분할 패킷 전송)
    public byte[] SendDataPacket(ushort seqNo, byte[] data, bool isFinal)
    {
        OpCode = isFinal ? OpCode.SplitTransferFinal : OpCode.SplitTransferInProgress;  // 마지막 패킷 여부에 따라 OpCode 설정
        SeqNo = seqNo;  // 순차 번호 설정
        Body = data;  // 전송할 데이터 설정
        Length = (uint)(data != null ? data.Length : 0);  // 데이터 길이 설정
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 전송 완료 응답 패킷 생성
    public byte[] TransferCompletionResponse(bool success)
    {
        OpCode = success ? OpCode.ReceptionCompletedSuccessfully : OpCode.ReceptionFailedAndConnectionClosed;  // 성공 여부에 따른 OpCode 설정
        Length = 0;
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }  
    // 파일 목록 요청 패킷 생성
    public byte[] GetFileListRequest()
    {
        OpCode = OpCode.FileListRequest;  // 파일 목록 요청 코드
        Length = 0;
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 파일 목록 응답 패킷 생성
    public byte[] GetFileListResponse(string[] filenames)
    {
        OpCode = OpCode.FileListResponse;  // 파일 목록 응답 코드
        string fileList = string.Join("\0", filenames);  // 파일명들을 null 문자로 구분하여 결합
        Body = Encoding.UTF8.GetBytes(fileList);  // 파일 목록을 바디에 저장
        Length = (uint)Body.Length;  // 바디의 길이 저장
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 파일 다운로드 요청 패킷 생성
    public byte[] DownloadFileRequest(string filename)
    {
        OpCode = OpCode.FileDownloadRequest;  // 파일 다운로드 요청 코드
        Body = Encoding.UTF8.GetBytes(filename);  // 파일명을 바디에 저장
        Length = (uint)Body.Length;  // 바디의 길이 저장
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 파일 다운로드 응답 패킷 생성
    public byte[] DownloadFileResponse(bool ok, uint filesize = 0)
    {
        OpCode = ok ? OpCode.FileDownloadOK : OpCode.FileDownloadFailed_FileNotFound;  // 파일 다운로드 성공/실패 여부에 따라 OpCode 설정
        if (ok)
        {
            Body = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)filesize));  // 파일 크기를 바디에 저장
            Length = 4;  // 바디의 길이 저장
        }
        else
        {            
            Length = 0;
            Body = null;
        }
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 파일 데이터 패킷 전송
    public byte[] SendFileDataPacket(ushort seqNo, byte[] data, bool isFinal)
    {
        OpCode = isFinal ? OpCode.FileDownloadDataEnd : OpCode.FileDownloadData;  // 파일 데이터 전송 여부에 따른 OpCode 설정
        SeqNo = seqNo;  // 순차 번호 설정
        Body = data;  // 전송할 데이터 설정
        Length = (uint)(data != null ? data.Length : 0);  // 데이터 길이 설정
        return GetPacket();  // 패킷 생성 및 반환
    }
    // 연결 종료 요청 패킷 생성
    public byte[] DisconnectionRequest()
    {
        OpCode = OpCode.RequestTerminationAfterProcessing;  // 연결 종료 요청 코드 (400)
        Length = 0;
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 연결 종료 응답 패킷 생성
    public byte[] DisconnectionResponse()
    {
        OpCode = OpCode.TerminationApprovedAndConnectionClosed;  // 연결 종료 응답 코드 (500)
        Length = 0;
        Body = null;
        return GetPacket();  // 패킷 생성 및 반환
    }

    // 패킷 정보를 콘솔에 출력하는 메서드
    public void PrintPacketInfo(string action)
    {
        Console.WriteLine($"[{action}] OpCode: {OpCode} ({(int)OpCode}) / SeqNo: {SeqNo} / Length: {Length}");                        
    }

    
}

// FTP 프로토콜에서 사용하는 명령 코드를 정의한 enum
public enum OpCode
{
    // 000: 접속 요청
    ConnectionRequest = 0,

    // 001: 접속 OK
    ConnectionOK = 1,

    // 002: 접속 Reject
    ConnectionReject = 2,

    // 100: 파일 전송 요청
    FileTransferRequest = 100,

    // 101: 파일 전송 OK
    FileTransferOK = 101,

    // 102: 파일명으로 전송 실패
    FileTransferFailed_FileName = 102,

    // 103: 파일크기로 전송 실패
    FileTransferFailed_FileSize = 103,

    //----------------------------------------
    // Client to Server (Download)

    // 105: 파일 목록 요청
    FileListRequest = 105,

    // 106: 파일 목록 응답
    FileListResponse = 106,

    // 110: 파일 다운로드 요청
    FileDownloadRequest = 110,

    // 111: 파일 다운로드 OK
    FileDownloadOK = 111,

    // 112: 파일 다운로드 실패 (파일 없음)
    FileDownloadFailed_FileNotFound = 112,

    // 120: 파일 데이터 전송 (다운로드)
    FileDownloadData = 120,

    // 121: 파일 데이터 전송 완료 (다운로드)
    FileDownloadDataEnd = 121,
    //----------------------------------------

    // 200: 분할 전송 시작과 전송 중
    SplitTransferInProgress = 200,

    // 210: 분할 전송 마지막
    SplitTransferFinal = 210,

    // 300: 수신 정상 완료
    ReceptionCompletedSuccessfully = 300,

    // 301: 수신 비정상 완료 & 접속 종료
    ReceptionFailedAndConnectionClosed = 301,

    // 400: 정상 처리 완료 후 종료 요청
    RequestTerminationAfterProcessing = 400,

    // 500: 정상 종료 승인 & 접속 종료
    TerminationApprovedAndConnectionClosed = 500
}
