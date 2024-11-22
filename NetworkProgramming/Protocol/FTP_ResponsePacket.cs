using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    // 응답하는 패킷 
    public class FTP_ResponsePacket
    {
        private readonly FTP _ftpProtocol;

        public FTP_ResponsePacket(FTP ftpProtocol)
        {
            _ftpProtocol = ftpProtocol;
        }
        // 연결 응답 패킷 생성
        public byte[] StartConnectionResponse(bool ok)
        {
            _ftpProtocol.OpCode = ok ? OpCode.ConnectionOK : OpCode.ConnectionReject;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }

        // 메세지 모드 변경 응답 패킷 생성
        public byte[] ChangeMessageModeResponse(bool ok)
        {
            _ftpProtocol.OpCode = ok ? OpCode.MessageModeOK : OpCode.MessageModeReject;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }

        // 로그인 요청 응답 패킷 생성
        public byte[] LoginResoponse(bool ok, OpCode errorCode = OpCode.LoginFailed_PW)
        {
            _ftpProtocol.OpCode = ok ? OpCode.LoginOK : errorCode;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;

            return _ftpProtocol.GetPacket();
        }

        // 회원가입 요청 응답 패킷 생성
        public byte[] RegisterResoponse(bool ok, OpCode errorCode = OpCode.RegisterFailed)
        {
            _ftpProtocol.OpCode = ok ? OpCode.RegisterOK : errorCode;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;

            return _ftpProtocol.GetPacket();
        }

        // 아이디 중복 확인 응답 패킷 생성
        public byte[] DuplicateCheckResoponse(bool ok, OpCode errorCode = OpCode.DuplicateCheckFailed)
        {
            _ftpProtocol.OpCode = ok ? OpCode.DuplicateCheckOK : errorCode;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;

            return _ftpProtocol.GetPacket();
        }

        // 파일 전송해도 되는지 응답 패킷 생성
        public byte[] TransmitFileResponse(bool ok, OpCode errorCode = OpCode.FileTransferOK)
        {
            _ftpProtocol.OpCode = ok ? OpCode.FileTransferOK : errorCode;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }

        // 파일 목록 보여달라는 응답 패킷 생성
        public byte[] GetFileListResponse(string[] filenames)
        {
            // 디렉토리의 파일 목록 배열을 받아온 상태
            _ftpProtocol.OpCode = OpCode.FileListResponse;
            string fileList = string.Join("\0", filenames);      // 파일 이름 배열을 file1\0file2\0.. 형태로 바꿈
            _ftpProtocol.Body = Encoding.UTF8.GetBytes(fileList);
            _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            return _ftpProtocol.GetPacket();
        }

        // 파일 다운로드 응답 패킷 생성
        public byte[] DownloadFileResponse(bool ok, uint filesize = 0, string fileHash = null)
        {
            _ftpProtocol.OpCode = ok ? OpCode.FileDownloadOK : OpCode.FileDownloadFailed_FileNotFound;

            if (ok)
            {
                byte[] fileSizeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)filesize));
                byte[] fileHashBytes = Encoding.UTF8.GetBytes(fileHash ?? string.Empty).Take(64).ToArray();

                _ftpProtocol.Body = new byte[4 + fileHashBytes.Length];
                Array.Copy(fileSizeBytes, 0, _ftpProtocol.Body, 0, fileSizeBytes.Length);
                Array.Copy(fileHashBytes, 0, _ftpProtocol.Body, fileSizeBytes.Length, fileHashBytes.Length);

                _ftpProtocol.Length = (uint)_ftpProtocol.Body.Length;
            }
            else
            {
                _ftpProtocol.Length = 0;
                _ftpProtocol.Body = null;
            }

            return _ftpProtocol.GetPacket();
        }
        // 전송 완료 응답 패킷 생성
        public byte[] TransferCompletionResponse(bool success)
        {
            _ftpProtocol.OpCode = success ? OpCode.ReceptionCompletedSuccessfully : OpCode.ReceptionFailedAndConnectionClosed;  // 성공 여부에 따른 OpCode 설정
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();  // 패킷 생성 및 반환
        }
        // 연결 종료 응답 패킷 생성
        public byte[] DisconnectionResponse()
        {
            _ftpProtocol.OpCode = OpCode.TerminationApprovedAndConnectionClosed;
            _ftpProtocol.Length = 0;
            _ftpProtocol.Body = null;
            return _ftpProtocol.GetPacket();
        }
        // 패킷 정보를 콘솔에 출력하는 메서드
        public void PrintPacketInfo(string action)
        {
            Console.WriteLine($"[{action}] OpCode: {_ftpProtocol.OpCode} ({(int)_ftpProtocol.OpCode}) / SeqNo: {_ftpProtocol.SeqNo} / Length: {_ftpProtocol.Length}");
        }

       
    }
}
