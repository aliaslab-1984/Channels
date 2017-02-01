using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels.RabbitMQ.Configuration
{
    public class ChannelsRabbitMQConfigurationSection : ConfigurationSection
    {
        #region nested types

        public class BrokerElement : ConfigurationElement
        {
            [ConfigurationProperty("requestedHearthbeatSeconds", DefaultValue = "60")]
            public ushort RequestedHeartbeatSeconds { get { return Convert.ToUInt16(this["requestedHearthbeatSeconds"]); } }

            [ConfigurationProperty("requestedFrameMaxBytes", DefaultValue = "0")]
            public uint RequestedFrameMaxBytes { get { return Convert.ToUInt32(this["requestedFrameMaxBytes"]); } }

            [ConfigurationProperty("continuationTimeoutMillis", DefaultValue = "10000")]
            public uint ContinuationTimeoutMillis { get { return Convert.ToUInt32(this["continuationTimeoutMillis"]); } }

            [ConfigurationProperty("handshakeContinuationTimeoutMillis", DefaultValue = "10000")]
            public uint HandshakeContinuationTimeoutMillis { get { return Convert.ToUInt32(this["handshakeContinuationTimeoutMillis"]); } }

            [ConfigurationProperty("networkRecoveryIntervalMillis", DefaultValue = "30000")]
            public uint NetworkRecoveryIntervalMillis { get { return Convert.ToUInt32(this["networkRecoveryIntervalMillis"]); } }

            [ConfigurationProperty("requestedChannelMaxCount", DefaultValue = "0")]
            public ushort RequestedChannelMaxCount { get { return Convert.ToUInt16(this["requestedChannelMaxCount"]); } }

            [ConfigurationProperty("requestedConnectionTimeoutMillis", DefaultValue = "60000")]
            public int RequestedConnectionTimeoutMillis { get { return Convert.ToInt32(this["requestedConnectionTimeoutMillis"]); } }

            [ConfigurationProperty("socketReadTimeoutMillis", DefaultValue = "10000")]
            public int SocketReadTimeoutMillis { get { return Convert.ToInt32(this["socketReadTimeoutMillis"]); } }

            [ConfigurationProperty("socketWriteTimeoutMillis", DefaultValue = "10000")]
            public int SocketWriteTimeoutMillis { get { return Convert.ToInt32(this["socketWriteTimeoutMillis"]); } }
        }

        public class HostNameCollection : ConfigurationElementCollection
        {
            public class HostNameElement : ConfigurationElement
            {
                [ConfigurationProperty("name", IsRequired = true)]
                public string Name { get { return this["name"].ToString(); } }
            }

            protected override ConfigurationElement CreateNewElement()
            {
                return new HostNameElement();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return ((HostNameElement)element).Name;
            }
        }

        public class ConnectionElement : ConfigurationElement
        {
            [ConfigurationProperty("hostName", IsRequired = true)]
            public string HostName { get { return this["hostName"].ToString(); } }

            [ConfigurationProperty("hostNamesPool", IsRequired = false)]
            public HostNameCollection HostNamesPool { get { return (HostNameCollection)this["hostNamesPool"]; } }

            [ConfigurationProperty("virtualHost", DefaultValue = "/")]
            public string VirtualHost { get { return this["virtualHost"].ToString(); } }

            [ConfigurationProperty("port", DefaultValue = "5672")]
            public int Port { get { return Convert.ToInt32(this["port"]); } }

            [ConfigurationProperty("userName", DefaultValue = "guest")]
            public string UserName { get { return this["userName"].ToString(); } }

            [ConfigurationProperty("password", DefaultValue = "guest")]
            public string Password { get { return this["password"].ToString(); } }
        }

        public class TLSElement : ConfigurationElement
        {
        }

        #endregion

        [ConfigurationProperty("broker")]
        public BrokerElement BrokerSettings { get { return (BrokerElement)this["broker"]; } }

        [ConfigurationProperty("connection", IsRequired = true)]
        public ConnectionElement ConnectionSettings { get { return (ConnectionElement)this["connection"]; } }

        [ConfigurationProperty("tls", IsRequired = true)]
        public TLSElement TLSSettings { get { return (TLSElement)this["tls"]; } }
    }
}
