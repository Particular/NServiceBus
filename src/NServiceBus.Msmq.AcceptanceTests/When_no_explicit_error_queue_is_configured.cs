namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using Transport;

    public class When_no_explicit_error_queue_is_configured : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_start_endpoint()
        {
            var ex = Assert.ThrowsAsync<Exception>(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithNoErrorQConfig>()
                    .Run();
            });

            StringAssert.Contains("Faults forwarding requires an error queue to be specified", ex.Message);
        }

        public class EndpointWithNoErrorQConfig : EndpointConfigurationBuilder
        {
            public EndpointWithNoErrorQConfig()
            {
                EndpointSetup<PlainVanillaEndpoint>();
            }
        }

        public class Context : ScenarioContext
        {
        }

        public class PlainVanillaEndpoint : IEndpointSetupTemplate
        {
            public PlainVanillaEndpoint()
            {
                typesToInclude = new List<Type>();
            }

            public PlainVanillaEndpoint(List<Type> typesToInclude)
            {
                this.typesToInclude = typesToInclude;
            }

#pragma warning disable CS0618
            public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
#pragma warning restore CS0618
            {
                var types = endpointConfiguration.GetTypesScopedByTestClass();

                typesToInclude.AddRange(types);

                var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

                configuration.TypesToIncludeInScan(typesToInclude);
                configuration.EnableInstallers();

                configuration.UseTransport<MsmqTransport>();

                var queueBindings = configuration.GetSettings().Get<QueueBindings>();
                runDescriptor.OnTestCompleted(_ => DeleteQueues(queueBindings));

                configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

                configuration.UsePersistence<InMemoryPersistence>();

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

            List<Type> typesToInclude;
        }
    }
}