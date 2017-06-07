﻿//
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//

using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Microsoft.Web.Redis.FunctionalTests
{
    internal class RedisServer : IDisposable
    {
        protected Process _server;

        protected void WaitForRedisToStart(int port = 6379)
        {
            if (port == 26379)
            {
                Thread.Sleep(1500); //Needs to wait sentinel estaiblish connection between Master & Slave
            }
            else
            {
              // if redis is not up in 2 seconds time than return failure
                for (int i = 0; i < 200; i++)
                {
                    Thread.Sleep(10);
                    try
                    {
                        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.Connect("localhost", port);
                        socket.Close();
                        LogUtility.LogInfo("Successful started redis server after Time: {0} ms", (i + 1)*10);
                        break;
                    }
                    catch {}
                }
            }
        }

        public RedisServer()
        {
            KillRedisServers();
            _server = new Process();
            _server.StartInfo.FileName = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-server.exe";
            _server.StartInfo.Arguments = "--maxmemory 20000000";
            _server.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            _server.Start();
            WaitForRedisToStart();
        }

        public RedisServer(string configFile, int port = 6379, bool sentinel = false)
        {
            _server = new Process();
            _server.StartInfo.FileName = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-server.exe";
            _server.StartInfo.Arguments = !sentinel ? $" {configFile}" : $" {configFile} --sentinel";
            _server.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            _server.Start();
            WaitForRedisToStart(port);
        }

        // Make sure that no redis-server instance is running
        private void KillRedisServers()
        {
            foreach (var proc in Process.GetProcessesByName("redis-server"))
            {
                try
                {
                    proc.Kill();
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (_server != null)
                {
                    _server.Kill();
                }
            }
            catch
            { }
        }
    }
}
