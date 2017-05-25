using System.Net;
using StackExchange.Redis;

namespace Microsoft.Web.Redis
{
    public class StackExchangeSentinelConnection
    {
        private ConnectionMultiplexer sentinel;
        private ConfigurationOptions sentinelConfiguration;

        public StackExchangeSentinelConnection(string connectionString)
        {
            var config = ConfigurationOptions.Parse(connectionString);

            sentinelConfiguration = new ConfigurationOptions
            {
                CommandMap = CommandMap.Sentinel,
                TieBreaker = "",
                ServiceName = config.ServiceName,
            };
            foreach (var endPoint in config.EndPoints)
            {
                sentinelConfiguration.EndPoints.Add(endPoint);
            }
            sentinel = ConnectionMultiplexer.Connect(sentinelConfiguration);
        }

        public void Close()
        {
            sentinel?.Close();
        }

        public EndPoint GetMasterAddressByName(string serviceName)
        {
            EndPoint masterEndPoint = null;
            foreach (var sentinelEndPoint in sentinelConfiguration.EndPoints)
            {
                try
                {
                    masterEndPoint =
                        sentinel.GetServer(sentinelEndPoint)
                            .SentinelGetMasterAddressByName(sentinelConfiguration.ServiceName);
                }
                catch
                {
                    // ignored
                }

                if (masterEndPoint != null)
                {
                    break;
                }
            }

            return masterEndPoint;
        }
    }
}