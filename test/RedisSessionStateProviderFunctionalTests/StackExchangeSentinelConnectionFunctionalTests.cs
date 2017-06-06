using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Redis.Tests;
using Xunit;

namespace Microsoft.Web.Redis.FunctionalTests
{
    public class StackExchangeSentinelConnectionFunctionalTests: IDisposable
    {
        private const string CONFIG_FILEPATH = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\{0}.conf";
        private const string MASTER_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-master.windows.conf";
        private const string SLAVE_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-slave.windows.conf";
        private const string SENTINEL_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\sentinel.windows.conf";

        private string _redisMaster = "";
        private string _redisSlave = "";
        private string _sentinel = "";

        [Fact]
        public void SentinelFailover()
        {
            InitRedisConf();
            KillRedisServers();

            using (var master = new RedisServer(_redisMaster))
            using (var slave = new RedisServer(_redisSlave, 6380))
            using (var sentinel = new RedisServer(_sentinel, 26379, true))
            {
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.ConnectionString = "127.0.0.1:26379, ServiceName=mymaster";

                var connection = new StackExchangeSentinelConnection(configuration.ConnectionString);
                var masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                var masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6379", masterConnectionString);

                //Block Redis Master 6s to force Sentinel failover
                BlockRedis(6379, 6);
                Thread.Sleep(5000); //wait for Sentinel failover done

                masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6380", masterConnectionString);

                Thread.Sleep(6000); //Wait 6s till the blocking be released

                //Block new Redis Master 6s to force Sentinel failover again
                BlockRedis(6380, 6);
                Thread.Sleep(5000); //wait for Sentinel failover done

                masterEndPoint = connection.GetMasterAddressByName("mymaster") as IPEndPoint;
                masterConnectionString = $"{masterEndPoint.Address}:{masterEndPoint.Port}";

                Assert.Equal("127.0.0.1:6379", masterConnectionString);

                connection.Close();
            }
        }

        [Fact]
        public void GetMasterAddressByName_Valid()
        {
            InitRedisConf();
            KillRedisServers();
            using (var master = new RedisServer(_redisMaster))
            using (var sentinel =new RedisServer(_sentinel, 26379, true))
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

        private void InitRedisConf()
        {
            _redisMaster = string.Format(CONFIG_FILEPATH, "redis-master-test.windows");
            _redisSlave = string.Format(CONFIG_FILEPATH, "redis-slave-test.windows");
            _sentinel = string.Format(CONFIG_FILEPATH, "sentinel-test.windows");

            File.Copy(MASTER_CONFIG_FILE, _redisMaster, true);
            File.Copy(SLAVE_CONFIG_FILE, _redisSlave, true);
            File.Copy(SENTINEL_CONFIG_FILE, _sentinel, true);

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

        public void Dispose()
        {
            File.Delete(_redisMaster);
            File.Delete(_redisSlave);
            File.Delete(_sentinel);
        }
    }
}
