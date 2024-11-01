﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLibrary
{
    public class ClientInfo
    {
        public TcpClient Client { get; }
        public int Id { get; set; }
        public string IpAddress { get; }
        public int Port { get; }
        public bool IsConnected { get; set; }
        public DateTime LastSendMessageTime { get; set; }

        // 접속시간, 총 접속시간 
        //public DateTime ConnectedTime { get; }
        //public TimeSpan TotalConnectedTime { get; set; }

        // 수신 및 송신 메시지 카운트
        public int ReceiveMessageCount { get; private set; }
        public int SendMessageCount { get; private set; }

        public ClientInfo(TcpClient client)
        {
            Client = client;
            IPEndPoint clientEp = (IPEndPoint)client.Client.RemoteEndPoint;
            IpAddress = clientEp.Address.ToString();
            Port = clientEp.Port;
            IsConnected = true;
            //ConnectedTime = DateTime.Now;            
        }

        public void Disconnect()
        {
            IsConnected = false;
            Client.Close();
            //TotalConnectedTime = DateTime.Now - ConnectedTime;
        }

        public void IncreaseReciveMessagCount()
        {
            ReceiveMessageCount++;
        }

        public void IncreaseSendMessageCount()
        {
            SendMessageCount++;
        }
    }
}
