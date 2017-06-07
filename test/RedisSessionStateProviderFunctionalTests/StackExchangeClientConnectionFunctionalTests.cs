using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.Web.Redis.Tests;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.Web.Redis.FunctionalTests
{
    public class StackExchangeClientConnectionFunctionalTests
    {
        private const string MASTER_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-master.windows.conf";
        private const string SLAVE_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\redis-slave.windows.conf";
        private const string SENTINEL_CONFIG_FILE = "..\\..\\..\\..\\..\\..\\packages\\redis-64.3.0.503\\tools\\sentinel.windows.conf";

        [Fact]
        public void Constructor_DatabaseIdFromConfigurationProperty()
        {
            using (RedisServer redisServer = new RedisServer())
            {
                int databaseId = 7;
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.DatabaseId = databaseId;

                StackExchangeClientConnection connection = new StackExchangeClientConnection(configuration);

                Assert.Equal(databaseId, connection.RealConnection.Database);

                connection.Close();
            }
        }

        [Fact]
        public void Constructor_DatabaseIdFromConnectionString()
        {
            using (RedisServer redisServer = new RedisServer())
            {
                int databaseId = 3;
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.ConnectionString = string.Format("localhost, defaultDatabase={0}", databaseId);

                StackExchangeClientConnection connection = new StackExchangeClientConnection(configuration);

                Assert.Equal(databaseId, connection.RealConnection.Database);

                connection.Close();
            }
        }

        [Fact]
        public void Constructor_DatabaseIdFromConfigurationPropertyWhenNotSetInConnectionString()
        {
            using (RedisServer redisServer = new RedisServer())
            {
                int databaseId = 5;
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.DatabaseId = databaseId;
                configuration.ConnectionString = string.Format("localhost");

                StackExchangeClientConnection connection = new StackExchangeClientConnection(configuration);

                Assert.Equal(databaseId, connection.RealConnection.Database);

                connection.Close();
            }
        }

        [Fact]
        public void Constructor_SetConnectionStringFromSentinel()
        {
            using (var master = new RedisServer(MASTER_CONFIG_FILE))
            using (var sentinel = new RedisServer(SENTINEL_CONFIG_FILE, 26379, true))
            {
                int databaseId = 5;
                ProviderConfiguration configuration = Utility.GetDefaultConfigUtility();
                configuration.DatabaseId = databaseId;
                configuration.ConnectionString = "127.0.0.1:26379, ServiceName=mymaster";

                var sentinelConnection = new StackExchangeSentinelConnection(configuration.ConnectionString);
                var connection = new StackExchangeClientConnection(configuration, sentinelConnection);

                Assert.Equal(databaseId, connection.RealConnection.Database);

                connection.Close();
            }
        }
    }
}