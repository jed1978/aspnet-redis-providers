using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Redis.Tests;
using Xunit;

namespace Microsoft.Web.Redis.FunctionalTests
{
    public class StackExchangeSentinelConnectionFunctionalTests
    {
        private const string MASTER_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-master.windows.conf";
        private const string SLAVE_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-slave.windows.conf";
        private const string SENTINEL_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\sentinel.windows.conf";

        [Fact]
        public void SentinelFailover()
        {
            KillRedisServers();

            using (var master = new RedisServer(MASTER_CONFIG_FILE))
            using (var slave = new RedisServer(SLAVE_CONFIG_FILE))
            using (var sentinel = new RedisServer(SENTINEL_CONFIG_FILE, true))
            {
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.ConnectionString = "127.0.0.1:26379, ServiceName=mymaster";

                var connection = new StackExchangeSentinelConnection(configuration.ConnectionString);
                var masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                var masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6379", masterConnectionString);

                //Block Redis Master 10s to trigger Sentinel failover
                BlockRedis(6379, 10);
                Thread.Sleep(5000); //wait for Sentinel failover 

                masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6380", masterConnectionString);

                Thread.Sleep(10000); //Wait 10s till the blocking be released

                //Block new Redis Master 10s to trigger Sentinel failover again
                BlockRedis(6380, 10);
                Thread.Sleep(5000); //wait for Sentinel failover

                masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6379", masterConnectionString);

                connection.Close();
            }
        }

        [Fact]
        public void GetMasterAddressByName_Valid()
        {
            KillRedisServers();
            using (var master =new RedisServer(MASTER_CONFIG_FILE))
            using (var sentinel =new RedisServer(SENTINEL_CONFIG_FILE,true))
            {
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.ConnectionString = "127.0.0.1:26379, ServiceName=mymaster";

                var connection = new StackExchangeSentinelConnection(configuration.ConnectionString);
                var masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                var masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6379", masterConnectionString);
            }
        }

        private static void BlockRedis(int port, int sleep)
        {
            Process redisCli = new Process();
            redisCli.StartInfo.FileName = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-cli.exe";
            redisCli.StartInfo.Arguments = $"-p {port} DEBUG sleep {sleep}";
            redisCli.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            redisCli.Start();
        }

        private static void KillRedisServers()
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
    }
}
