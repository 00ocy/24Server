using System.Net.Sockets;
using System.Net;
using System.Text;

namespace console_udp_03
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // multicast address
            string MULTICASTIP = "224.0.0.10";
            int LOCALPORT = 9000;

            var remoteEP = new IPEndPoint(IPAddress.Parse(MULTICASTIP), LOCALPORT);
            sendMulticast(remoteEP);
        }
        private static void sendMulticast(IPEndPoint remoteEP)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // send setsocketoption
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);


            string data = "You must study network ~!!!!!!\n";
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            //byte[] buffer = null;
            while (true)
            {
                sock.SendTo(buffer, buffer.Length, SocketFlags.None, remoteEP);
                Console.WriteLine($"Send Data to multicasting ip {remoteEP.Address.ToString()}");
                Thread.Sleep(1000);
            }

        }

    }
}
