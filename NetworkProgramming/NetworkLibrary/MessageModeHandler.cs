using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLibrary
{
    public class MessageModeHandler
    {
        private NetworkStream _stream;

        public MessageModeHandler(NetworkStream stream)
        {
            _stream = stream;
        }

        public void SendMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            _stream.Write(data, 0, data.Length);
        }

        public string ReceiveMessage()
        {
            using (StreamReader reader = new StreamReader(_stream, Encoding.UTF8, true, 1024, true))
            {
                return reader.ReadLine();
            }
        }
    }

}
