using Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpClientTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FtpClient ftpClient = new FtpClient("172.18.27.201", 25000);
            ftpClient.ConnectServer();
        }
    }

}