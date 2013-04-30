using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Config;
using NServiceBus.Features;
using NServiceBus.Saga;
using NServiceBus.Timeout.Core;
using NServiceBus.Unicast.Queuing.Windows.ServiceBus;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace NServiceBus.Transports.WindowsServiceBus
{
	class WindowsServiceBusTransport : ConfigureTransport<NServiceBus.WindowsServiceBus>, IFeature
	{
		protected override void InternalConfigure( Configure config, string connectionString )
		{
			var configSection = NServiceBus.Configure.GetConfigSection<WindowsServiceBusQueueConfig>() 
				?? new WindowsServiceBusQueueConfig();

			ServiceBusEnvironment.SystemConnectivity.Mode = ( ConnectivityMode )Enum.Parse( typeof( ConnectivityMode ), configSection.ConnectivityMode );

			var namespaceClient = NamespaceManager.CreateFromConnectionString( connectionString );
			var serviceUri = namespaceClient.Address;
			var factory = MessagingFactory.CreateFromConnectionString( connectionString );

			Address.OverrideDefaultMachine( serviceUri.ToString() );

			config.Configurer.RegisterSingleton<NamespaceManager>( namespaceClient );
			config.Configurer.RegisterSingleton<MessagingFactory>( factory );

			// make sure the transaction stays open a little longer than the long poll.
			NServiceBus.Configure.Transactions.Advanced( settings => settings.DefaultTimeout( TimeSpan.FromSeconds( configSection.ServerWaitTime * 1.1 ) ).IsolationLevel( IsolationLevel.Serializable ) );

			if ( !string.IsNullOrEmpty( configSection.QueueName ) )
			{
				NServiceBus.Configure.Instance.DefineEndpointName( configSection.QueueName );
			}

			Address.InitializeLocalAddress( NServiceBus.Configure.EndpointName );

			if ( !config.Configurer.HasComponent<IDequeueMessages>() )
			{
				config.Configurer.ConfigureComponent<WindowsServiceBusDequeueStrategy>( DependencyLifecycle.InstancePerCall );
			}

			if ( !config.Configurer.HasComponent<ISendMessages>() )
			{
				config.Configurer.ConfigureComponent<WindowsServiceBusMessageQueueSender>( DependencyLifecycle.InstancePerCall );
				config.Configurer.ConfigureProperty<WindowsServiceBusMessageQueueSender>( t => t.MaxDeliveryCount, configSection.MaxDeliveryCount );

				if ( config.Configurer.HasComponent<WindowsServiceBusDequeueStrategy>() )
				{
					config.Configurer.ConfigureComponent<WindowsServiceBusQueueNotifier>( DependencyLifecycle.InstancePerCall );
					config.Configurer.ConfigureComponent<WindowsServicebusQueueClientCreator>( DependencyLifecycle.InstancePerCall );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.LockDuration, TimeSpan.FromMilliseconds( configSection.LockDuration ) );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.RequiresSession, configSection.RequiresSession );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds( configSection.DefaultMessageTimeToLive ) );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds( configSection.DuplicateDetectionHistoryTimeWindow ) );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.MaxDeliveryCount, configSection.MaxDeliveryCount );
					config.Configurer.ConfigureProperty<WindowsServicebusQueueClientCreator>( t => t.EnableBatchedOperations, configSection.EnableBatchedOperations );
					config.Configurer.ConfigureProperty<WindowsServiceBusQueueNotifier>( t => t.ServerWaitTime, configSection.ServerWaitTime );
					config.Configurer.ConfigureProperty<WindowsServiceBusQueueNotifier>( t => t.BatchSize, configSection.BatchSize );
					config.Configurer.ConfigureProperty<WindowsServiceBusQueueNotifier>( t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds );
				}
			}

			if ( !config.Configurer.HasComponent<IPublishMessages>() &&
				!config.Configurer.HasComponent<IManageSubscriptions>()
				&& config.Configurer.HasComponent<WindowsServiceBusDequeueStrategy>() )
			{
				config.Configurer.ConfigureComponent<WindowsServicebusSubscriptionClientCreator>( DependencyLifecycle.InstancePerCall );
				config.Configurer.ConfigureComponent<WindowsServiceBusTopicSubscriptionManager>( DependencyLifecycle.InstancePerCall );
				config.Configurer.ConfigureComponent<WindowsServiceBusTopicPublisher>( DependencyLifecycle.InstancePerCall );
				config.Configurer.ConfigureComponent<WindowsServiceBusTopicNotifier>( DependencyLifecycle.InstancePerCall );

				config.Configurer.ConfigureProperty<WindowsServiceBusTopicPublisher>( t => t.MaxDeliveryCount, configSection.MaxDeliveryCount );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.LockDuration, TimeSpan.FromMilliseconds( configSection.LockDuration ) );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.RequiresSession, configSection.RequiresSession );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds( configSection.DefaultMessageTimeToLive ) );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.EnableDeadLetteringOnFilterEvaluationExceptions, configSection.EnableDeadLetteringOnFilterEvaluationExceptions );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.MaxDeliveryCount, configSection.MaxDeliveryCount );
				config.Configurer.ConfigureProperty<WindowsServicebusSubscriptionClientCreator>( t => t.EnableBatchedOperations, configSection.EnableBatchedOperations );
				config.Configurer.ConfigureProperty<WindowsServiceBusTopicNotifier>( t => t.ServerWaitTime, configSection.ServerWaitTime );
				config.Configurer.ConfigureProperty<WindowsServiceBusTopicNotifier>( t => t.BatchSize, configSection.BatchSize );
				config.Configurer.ConfigureProperty<WindowsServiceBusTopicNotifier>( t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds );
			}
		}

		protected override string ExampleConnectionStringForErrorMessage
		{
			get { return "Endpoint=sb://[machine-name]/ServiceBusDefaultNamespace;StsEndpoint=https://[machine-name]:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355"; }
		}

		public void Initialize()
		{
			Feature.Enable<WindowsServiceBusTransport>();
			//Feature.Enable<MessageDrivenSubscriptions>();
			Feature.Enable<TimeoutManager>();

			InfrastructureServices.SetDefaultFor<ISagaPersister>( () => NServiceBus.Configure.Instance.RavenSagaPersister() );
			InfrastructureServices.SetDefaultFor<IPersistTimeouts>( () => NServiceBus.Configure.Instance.UseRavenTimeoutPersister() );
			InfrastructureServices.SetDefaultFor<ISubscriptionStorage>( () => NServiceBus.Configure.Instance.RavenSubscriptionStorage() );
		}
	}
}
