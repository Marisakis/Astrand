﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Networking.Server
{
    public class ClientConnection
    {
        private bool isConnected;

        private TcpClient client;
        private NetworkStream stream;
        private Thread listenerThread;

        public string Id { get; set; }

        private IClientDataReceiver receiver;
        private ILogger logger;

        private Server server;

        public ClientConnection(TcpClient client, Server server, IClientDataReceiver receiver, ILogger logger)
        {
            this.isConnected = false;

            this.client = client;
            this.stream = this.client.GetStream();

            this.receiver = receiver;
            this.logger = logger;
            this.server = server;
            this.Id = HashUtil.HashMD5(GetIp() + GetPort());

            InitilizeListenerThread();
        }

        private void InitilizeListenerThread()
        {
            this.listenerThread = new Thread(() =>
            {
                while (this.isConnected)
                {
                    byte[] bytes = Receive();
                    if (bytes.Length == 0)
                    {
                        Disconnect();
                        this.server.DiconnectClient(this);
                    }
                    else
                        this.receiver?.OnDataReceived(bytes, this.Id);
                }
            });
        }

        public bool Connect()
        {
            if(!this.isConnected)
            {
                this.listenerThread.Start();
                this.isConnected = true;
            }
            return true;
        }

        public void Disconnect()
        {
            if (this.isConnected)
            {
                this.isConnected = false;
                this.logger?.Log($"Client disconnected on {GetIp()} using port {GetPort()}\n");
                this.client.Close();
                this.stream.Close();
            }
        }

        public void Transmit(byte[] data)
        {
            if(data != null && this.isConnected)
            {
                byte[] sizeinfo = new byte[4];

                sizeinfo[0] = (byte)data.Length;
                sizeinfo[1] = (byte)(data.Length >> 8);
                sizeinfo[2] = (byte)(data.Length >> 16);
                sizeinfo[3] = (byte)(data.Length >> 24);

                this.stream.Write(sizeinfo, 0, sizeinfo.Length);
                this.stream.Write(data, 0, data.Length);
            }
        }

        public byte[] Receive()
        {
            try
            {
                byte[] sizeinfo = new byte[4];
                int totalread = 0, currentread = 0;

                currentread = totalread = this.stream.Read(sizeinfo, 0, sizeinfo.Length);

                while (totalread < sizeinfo.Length && currentread > 0)
                {
                    currentread = this.stream.Read(sizeinfo, totalread, sizeinfo.Length - totalread);
                    totalread += currentread;
                }

                int messagesize = 0;
                messagesize |= sizeinfo[0];
                messagesize |= (((int)sizeinfo[1]) << 8);
                messagesize |= (((int)sizeinfo[2]) << 16);
                messagesize |= (((int)sizeinfo[3]) << 24);

                byte[] buffer = new byte[messagesize];
                totalread = 0;

                currentread = totalread = this.stream.Read(buffer, totalread, buffer.Length - totalread);

                while (totalread < messagesize && currentread > 0)
                {
                    currentread = this.stream.Read(buffer, totalread, buffer.Length - totalread);
                    totalread += currentread;
                }

                if(this.logger != null)
                {
                    string responseData = Encoding.UTF8.GetString(buffer, 0, totalread);
                    if (responseData.Length != 0)
                        this.logger.Log($"Received: {responseData}\n");
                }

                return buffer;
            }
            catch(Exception e)
            {
                return new byte[0];
            }
        }

        public string GetIp()
        {
            return this.client.Client.RemoteEndPoint.ToString().Split(':')[0];
        }

        public int GetPort()
        {
            return int.Parse(this.client.Client.RemoteEndPoint.ToString().Split(':')[1]);
        }
    }
}
