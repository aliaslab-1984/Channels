using Channels.RabbitMQ.Configuration;
using log4net;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.RabbitMQ
{
    /// <summary>
    /// Singleton manager class for sensing.
    /// </summary>
    public class ChannelsRabbitMQManager
    {
        private static ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected static ChannelsRabbitMQManager _instance = null;

        /// <summary>
        /// Instance getter method.
        /// </summary>
        /// <returns>ProbeManager singleton instance.</returns>
        public static ChannelsRabbitMQManager Ask()
        {
            if (_instance == null)
                throw new InvalidOperationException("Component not initialized.");
            return _instance;
        }
        
        protected ChannelsRabbitMQManager(ChannelsRabbitMQConfigurationSection conf)
        {
            List<string> hostNames = null;

            ConnectionFactory factory = new ConnectionFactory();

            factory.AutomaticRecoveryEnabled = true;
            factory.TopologyRecoveryEnabled = true;
            factory.UseBackgroundThreadsForIO = false;
            
            if (conf != null)
            {
                if (conf.BrokerSettings != null)
                {
                    factory.RequestedHeartbeat = conf.BrokerSettings.RequestedHeartbeatSeconds;
                    factory.RequestedFrameMax = conf.BrokerSettings.RequestedFrameMaxBytes;
                    factory.ContinuationTimeout = TimeSpan.FromMilliseconds(conf.BrokerSettings.ContinuationTimeoutMillis);
                    factory.HandshakeContinuationTimeout = TimeSpan.FromMilliseconds(conf.BrokerSettings.HandshakeContinuationTimeoutMillis);

                    factory.NetworkRecoveryInterval = TimeSpan.FromMilliseconds(conf.BrokerSettings.NetworkRecoveryIntervalMillis);

                    factory.RequestedChannelMax = conf.BrokerSettings.RequestedChannelMaxCount;
                    factory.RequestedConnectionTimeout = conf.BrokerSettings.RequestedConnectionTimeoutMillis;
                    factory.SocketReadTimeout = conf.BrokerSettings.SocketReadTimeoutMillis;
                    factory.SocketWriteTimeout = conf.BrokerSettings.SocketWriteTimeoutMillis;
                }

                if (conf.ConnectionSettings != null)
                {
                    factory.HostName = conf.ConnectionSettings.HostName;
                    factory.VirtualHost = conf.ConnectionSettings.VirtualHost;
                    factory.Port = conf.ConnectionSettings.Port;

                    factory.UserName = conf.ConnectionSettings.UserName;
                    factory.Password = conf.ConnectionSettings.Password;

                    if (conf.ConnectionSettings.HostNamesPool != null && conf.ConnectionSettings.HostNamesPool.Count > 0)
                    {
                        hostNames = new List<string>();
                        foreach (ChannelsRabbitMQConfigurationSection.HostNameCollection.HostNameElement item in conf.ConnectionSettings.HostNamesPool)
                        {
                            hostNames.Add(item.Name);
                        }

                        if (!hostNames.Contains(conf.ConnectionSettings.HostName))
                            hostNames.Add(conf.ConnectionSettings.HostName);
                    }
                }

                if (conf.TLSSettings != null)
                {
                    //FUTURE: enable TLS support
                }
            }
            else
            {
                _logger.Info("Initialized, with defaults unchanged.");
            }

            if (hostNames!=null)
                Connection = factory.CreateConnection(hostNames);
            else
                Connection = factory.CreateConnection();

            Connection.ConnectionBlocked += (sender, ea) => {
                _logger.WarnFormat("RabbitMQ connection BLOCKED localPort: {0}, reason: {1}",Connection.LocalPort,ea.Reason);
            };
            Connection.ConnectionUnblocked += (sender, ea) => {
                _logger.InfoFormat("RabbitMQ connection UNBLOCKED localPort: {0}", Connection.LocalPort);
            };
            Connection.ConnectionShutdown += (sender, ea) => {
                _logger.FatalFormat("RabbitMQ connection SHUTDOWN localPort: {0}, details: {1}", Connection.LocalPort, ea.ToString());
            };
        }

        /// <summary>
        /// Inits the manager from configuration using the default configuration section tag delimiter (codeProbe).
        /// </summary>
        public static void Init()
        {
            Init("channelsRabbitMQ");
        }

        /// <summary>
        /// Inits the manager from configuration using a custom configuration section tag delimiter.
        /// </summary>
        /// <param name="section">Configuration section tag delimiter</param>
        public static void Init(string section)
        {
            _logger.Info("Initializing.");

            try
            {
                if (_instance != null)
                    throw new InvalidOperationException("Already initialized.");

                ChannelsRabbitMQConfigurationSection conf = (ChannelsRabbitMQConfigurationSection)System.Configuration.ConfigurationManager.GetSection(section);

                _instance = new ChannelsRabbitMQManager(conf);

                _logger.Info("Initialized.");
            }
            catch (Exception e)
            {
                _logger.Error("Error during intialization.", e);
                throw e;
            }
        }

        internal IConnection Connection { get; set; }

        internal string HeadersNameWriterClosed { get { return "channels-write-close"; } }
        internal string HeadersNameWriterMessageType { get { return "channels-write-messageType"; } }
        internal string ChannelCollectorExchange { get { return "channels-channelCollectorExchange"; } }
        internal string SubscribableChannelCollectorExchange { get { return "channels-subscribableChannelCollectorExchange"; } }
    }
}
