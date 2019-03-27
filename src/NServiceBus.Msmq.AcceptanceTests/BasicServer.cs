namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Configuration.AdvanceExtensibility;
    using Transport;

    public abstract class BasicServer : IEndpointSetupTemplate
    {
        protected abstract void ApplyConfig(EndpointConfiguration configuration);

        protected virtual IList<Type> ExtraTypesToInclude()
        {
            return new List<Type>();
        }

#pragma warning disable CS0618
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
#pragma warning restore CS0618
        {
            var types = endpointConfiguration.GetTypesScopedByTestClass().ToList();

            types.AddRange(ExtraTypesToInclude());

            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(types);
            configuration.CustomConfigurationSource(configSource);
            configuration.EnableInstallers();

            configuration.GetSettings().Set("NServiceBus.Transport.Msmq.MessageEnumeratorTimeout", TimeSpan.FromMilliseconds(10));

            var transportConfig = configuration.UseTransport<MsmqTransport>();
            var routingConfig = transportConfig.Routing();

            foreach (var publisher in endpointConfiguration.PublisherMetadata.Publishers)
            {
                foreach (var eventType in publisher.Events)
                {
                    routingConfig.RegisterPublisher(eventType, publisher.PublisherName);
                }
            }

            ApplyConfig(configuration);

            var queueBindings = configuration.GetSettings().Get<QueueBindings>();
            runDescriptor.OnTestCompleted(_ => DeleteQueues(queueBindings));

            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configuration.UsePersistence<InMemoryPersistence>();

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(configuration);

            return Task.FromResult(configuration);
        }

        Task DeleteQueues(QueueBindings queueBindings)
        {
            var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
            var queuesToBeDeleted = new List<string>();

            foreach (var messageQueue in allQueues)
            {
                using (messageQueue)
                {
                    if (queueBindings.ReceivingAddresses.Any(ra =>
                    {
                        var indexOfAt = ra.IndexOf("@", StringComparison.Ordinal);
                        if (indexOfAt >= 0)
                        {
                            ra = ra.Substring(0, indexOfAt);
                        }
                        return messageQueue.QueueName.StartsWith(@"private$\" + ra, StringComparison.OrdinalIgnoreCase);
                    }))
                    {
                        queuesToBeDeleted.Add(messageQueue.Path);
                    }
                }
            }

            foreach (var queuePath in queuesToBeDeleted)
            {
                try
                {
                    MessageQueue.Delete(queuePath);
                    Console.WriteLine("Deleted '{0}' queue", queuePath);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not delete queue '{0}'", queuePath);
                }
            }

            MessageQueue.ClearConnectionCache();

            return Task.FromResult(0);
        }
    }
}