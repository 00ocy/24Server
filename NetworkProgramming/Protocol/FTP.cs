using System.Net;

namespace Protocol
{

    // FTP 객체 초기화
    // FTP OpCode 관리
    // FTP 헤더 생성
    // 전체 패킷 생성 함수
    public class FTP
    {
        public byte ProtoVer { get; internal set; }          // 프로토콜 버전
        public OpCode OpCode { get; internal set; }          // 명령 코드 (OpCode enum 사용)
        public uint SeqNo { get; internal set; }             // 순차 번호 (데이터 분할 전송 시 사용)
        public uint Length { get; internal set; }            // 데이터 길이 (본문의 길이)
        public byte[]? Body { get; set; }                   // 데이터 본문 (null 허용)

        // FTP 객체 생성 시 초기화
        public FTP()
        {
            ProtoVer = 1;                 // 기본 프로토콜 버전 1
            OpCode = OpCode.ConnectionOK; // 기본 명령 코드 (ConnectionOK)
            SeqNo = 0;                    // 기본 순차 번호 0
            Length = 0;                   // 데이터 길이 0
            Body = null;                  // 본문은 없음
        }

        // 헤더 생성 메서드 (헤더에 필요한 정보들을 바이트 배열로 반환)
        private byte[] MakeHeader()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte(ProtoVer); // 프로토콜 버전 추가
                ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)OpCode)), 0, 2); // OpCode 추가 (2바이트)
                ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)SeqNo)), 0, 4);    // SeqNo 추가 (4바이트)
                ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Length)), 0, 4);   // Length 추가 (4바이트)
                return ms.ToArray();    // 완성된 헤더 반환
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

        // 005: 메세지 모드 변경 요청
        MessageModeRequest = 5,

        // 006: 메세지 모드 변경 OK
        MessageModeOK = 6,

        // 007: 메세지 모드 변경 거부
        MessageModeReject = 7,

        // 008: 메세지 전송 요청
        MessageRequest = 8, 

        // 010 : 로그인 요청
        LoginRequest =10,

        // 011 : 로그인 OK
        LoginOK = 11,

        // 012 : 로그인 실패_아이디
        LoginFailed_ID = 12,

        // 013 : 로그인 실패_비밀번호
        LoginFailed_PW = 13,

        // 020 : 아이디 중복 확인 요청
        DuplicateCheckRequest = 20,

        // 020 : 아이디 중복 확인 OK
        DuplicateCheckOK = 21,

        // 020 : 아이디 중복 확인 실패
        DuplicateCheckFailed = 22,

        // 030 : 회원가입 요청
        RegisterRequest = 30,

        // 031 : 회원가입 OK
        RegisterOK = 31,

        // 031 : 회원가입 실패
        RegisterFailed = 32,

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

        // 113: 파일 다운로드 실패 (SHA-256 해시값이 다름)
        FileDownloadFailed_SHAhashValueDifferent =113,

     
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

}