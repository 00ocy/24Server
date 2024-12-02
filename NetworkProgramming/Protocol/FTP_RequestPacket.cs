using SecurityLibrary;
using System.Text;

namespace Protocol
{
    // 요청하는 FTP 패킷 모음
    public class FTP_RequestPacket
    {
        private readonly FTP _ftpProtocol;

        public FTP_RequestPacket(FTP ftpProtocol)
        {
            _ftpProtocol = ftpProtocol;
        }
        // 연결 요청 패킷 생성
        public byte[] StartConnectionRequest()
        {
            _ftpProtocol.OpCode = OpCode.ConnectionRequest;  // 접속 요청 코드
            _ftpProtocol.Length = 0;                         // 바디가 없으므로 길이는 0
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();                 // 패킷 생성 및 반환
        }

       // 파일 전송 요청 패킷 생성 (해시 추가)
        public byte[] TransmitFileRequest(string filename, uint filesize, string fileHash)
        {
            _ftpProtocol.OpCode = OpCode.FileTransferRequest;
            string fileInfo = $"{filename}\0{filesize}\0{fileHash}";
            _ftpProtocol.Body = AESHelper.Encrypt(Encoding.UTF8.GetBytes(fileInfo));
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }
        

        // 파일 목록 요청 패킷 생성
        public byte[] GetFileListRequest()
        {
            _ftpProtocol.OpCode = OpCode.FileListRequest;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }

        // 파일 다운로드 요청 패킷 생성
        public byte[] DownloadFileRequest(string filename)
        {
            _ftpProtocol.OpCode = OpCode.FileDownloadRequest;
            _ftpProtocol.Body = AESHelper.Encrypt(Encoding.UTF8.GetBytes(filename));
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }

        // 연결 종료 요청 패킷 생성
        public byte[] DisconnectionRequest()
        {
            _ftpProtocol.OpCode = OpCode.RequestTerminationAfterProcessing;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }

        // 패킷 정보를 콘솔에 출력하는 메서드
        public void PrintPacketInfo(string action)
        {
            Console.WriteLine($"[{action}] OpCode: {_ftpProtocol.OpCode} ({(int)_ftpProtocol.OpCode}) / SeqNo: {_ftpProtocol.SeqNo} / Length: {_ftpProtocol.Length}");
        }

       /* public byte[] ChangeToMessageModeRequest()
        {
            _ftpProtocol.OpCode = OpCode.MessageModeRequest;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }*/

        // 메세지 전송
        public byte[] MessageSendRequest(string userInput)
        {
            _ftpProtocol.OpCode = OpCode.MessageRequest;
            // Body 데이터 암호화
            _ftpProtocol.Body = AESHelper.Encrypt(Encoding.UTF8.GetBytes(userInput));
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }

        // 로그인 요청 전송
        public byte[] LoginRequest(string encryptedIDPW)
        {
            // 암호화 된 유저 IDPW 담은 패킷
            _ftpProtocol.OpCode = OpCode.LoginRequest;

            // Body 데이터 암호화
            _ftpProtocol.Body = AESHelper.Encrypt(Encoding.UTF8.GetBytes(encryptedIDPW));
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }

        // 회원가입 요청 전송
        public byte[] RegisterRequest(string encryptedIDPW)
        {
            // 암호화 된 유저 IDPW 담은 패킷
            _ftpProtocol.OpCode = OpCode.RegisterRequest;
            // Body 데이터 암호화
            _ftpProtocol.Body = AESHelper.Encrypt(Encoding.UTF8.GetBytes(encryptedIDPW));
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }

        // 아이디 중복 확인 요청 전송
        public byte[] DuplicateCheckRequest(string encryptedId)
        {
            // 암호화 된 유저 ID 담은 패킷
            _ftpProtocol.OpCode = OpCode.DuplicateCheckRequest;
            // Body 데이터 암호화
            _ftpProtocol.Body = AESHelper.Encrypt(Encoding.UTF8.GetBytes(encryptedId));
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }
    }
}
