using System.Net;
using System.Net.Sockets;
using System.Text;

namespace console_tcpClient_variableType2
{
    internal class Client_variableType2
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[CLIENT] Send Message VariableType 2 With SizeData");
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 25000);
            SendMessageVariableTypeWithSizeData(remoteEP);
        }

        private static void SendMessageVariableTypeWithSizeData(IPEndPoint remoteEP)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 5);

            sock.Connect(remoteEP);

            // 사이즈+ 데이터 분리 전송용
            String sendCommand = "GET Weather/NewTemp";
            byte[] DataBuf = Encoding.UTF8.GetBytes(sendCommand);
            byte[] sizeBuf = new byte[sizeof(int)];
            sizeBuf = BitConverter.GetBytes(DataBuf.Length);

            // 1.분리전송
            // SizeBuf 전송
            sock.Send(sizeBuf, 0, sizeBuf.Length, SocketFlags.None);
            // DataBuf 전송
            sock.Send(DataBuf, 0, DataBuf.Length, SocketFlags.None);


        }
    }
}
