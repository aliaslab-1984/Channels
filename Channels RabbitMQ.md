#Channels RabbitMQ

The library is an ipmlementation of the abstractions provided by the base Channels library and in particular provides implementation of every _named channel_ interface.

## RabbitMQ configuration

For a correct functioning the followinf commands must be executed on the rabbitMQ nodes:

<pre>
rabbitmqctl add_user admin admin

rabbitmqctl set_user_tags admin administrator

rabbitmqctl add_vhost channels-lib

rabbitmqctl set_permissions -p channels-lib admin ".*" ".*" ".*"

rabbitmqctl set_policy -p channels-lib expiry ".*" "{\""expires"\":1800000}" --apply-to queues
</pre>

that means:

- add a user with name _admin_ and password _admin_
- create a vritual host named channels-lib
- assign every permission to admin for the virtual host channels-lib
- set the persistent queues expiry after 30 minutes of idleness

## Client application configuration

Application .NET .config modifications:

	<configSections>
	    <section name="channelsRabbitMQ" type="Channels.RabbitMQ.Configuration.ChannelsRabbitMQConfigurationSection, Channels.RabbitMQ"/>
	</configSections>

and the relative section (omit the values tou don't want to change from the rabbitMQ defaults):

	<channelsRabbitMQ>
	    <broker
	      requestedHearthbeatSeconds="1"
	      requestedFrameMaxBytes="2"
	      continuationTimeoutMillis="3"
	      handshakeContinuationTimeoutMillis="4"
	      networkRecoveryIntervalMillis="5"
	      requestedChannelMaxCount="6"
	      requestedConnectionTimeoutMillis="7"
	      socketReadTimeoutMillis="8"
	      socketWriteTimeoutMillis="9"
	        />
	    <connection
	      hostName="idsign.test.aliaslab.net"
	      virtualHost="channels-lib"
	      port="5672"
	      userName="admin"
	      password="admin"
	      >
	      <hostNamesPool>
	      </hostNamesPool>
	    </connection>
	  </channelsRabbitMQ>

and initialize the manager in the code:
<pre>
ChannelsRabbitMQManager.Init();
</pre>