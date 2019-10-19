﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Networking.Server
{
    public class Server
    {
        private bool isReady;
        private bool isRunning;

        private IPAddress host;
        private int port;

        private TcpListener listener;
        private Thread listenerThread;

        private IClientDataReceiver receiver;
        private IServerConnector connector;
        private ILogger logger;

        private List<ClientConnection> connections;

        public Server(string ip, int port, IClientDataReceiver receiver, IServerConnector connector, ILogger logger)
        {
            this.isReady = IPAddress.TryParse(ip, out this.host);
            this.isRunning = false;

            this.port = port;
            this.receiver = receiver;
            this.connector = connector;
            this.logger = logger;

            this.connections = new List<ClientConnection>();
        }

        private void InitilizeListenerThread()
        {
            this.listenerThread = new Thread(() =>
            {
                while (this.isRunning)
                {
                    if (!this.listener.Pending())
                    {
                        Thread.Sleep(200);
                        continue;
                    }
                    ClientConnection connection = new ClientConnection(this.listener.AcceptTcpClient(), this, this.receiver, this.logger);
                    this.connections.Add(connection);
                    connection.Connect();
                    this.connector?.OnClientConnected(connection);
                    this.logger?.Log($"Client connected on {connection.GetIp()} using port {connection.GetPort()}\n");
                }
            });
        }

        public bool Start()
        {
            if(this.isReady && !this.isRunning)
            {
                try
                {
                    this.isRunning = true;
                    this.listener = new TcpListener(this.host, this.port);
                    this.listener.Start();

                    InitilizeListenerThread();
                    this.listenerThread.Start();

                    this.logger?.Log($"Server started on {this.host} using port {this.port}\n");

                    return true;
                }
                catch(Exception e)
                {
                    return false;
                }
            }
            else if(!this.isReady)
            {
                this.logger?.Log("Server could not be started due to invalid ip!\n");
            }
            else
            {
                this.logger?.Log("Server is already running!\n");
            }
            return false;
        }

        public void Stop()
        {
            if (this.isRunning)
            {
                this.isRunning = false;
                foreach (ClientConnection connection in this.connections)
                {
                    connection.Disconnect();
                    this.connector?.OnClientDisconnected(connection);
                }
                this.connections.Clear();

                this.listener.Stop();
                this.logger?.Log($"Server stopped on {this.host} using port {this.port}\n");
            }
        }

        public void DiconnectClient(ClientConnection connection)
        {
            if(connection != null)
            {
                if(this.connections.Contains(connection))
                    this.connections.Remove(connection);
                this.connector?.OnClientDisconnected(connection);
            }
        }

        public void Transmit(byte[] data, string clientId)
        {
            ClientConnection connection = this.connections.Where(c => c.Id == clientId).First();
            if (connection != null)
                connection.Transmit(data);
        }

        public ClientConnection GetConnection(string clientId)
        {
            if (this.connections.Where(c => c.Id == clientId).Count() != 0)
                return this.connections.Where(c => c.Id == clientId).First();
            else
                return null;
        }

        public void SetLogger(ILogger logger)
        {
            this.logger = logger;
        }
    }
}
