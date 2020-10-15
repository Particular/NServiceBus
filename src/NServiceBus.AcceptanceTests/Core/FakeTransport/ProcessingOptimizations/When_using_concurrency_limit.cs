using System;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport.ProcessingOptimizations
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;
    using Transport;

    public class When_using_concurrency_limit : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_pass_it_to_the_transport()
        {
            return Scenario.Define<ScenarioContext>()
                .WithEndpoint<ThrottledEndpoint>(b => b.CustomConfig(c => c.LimitMessageProcessingConcurrencyTo(10)))
                .Done(c => c.EndpointsStarted)
                .Run();

            //Assert in FakeReceiver.Start
        }

        class ThrottledEndpoint : EndpointConfigurationBuilder
        {
            public ThrottledEndpoint()
            {
                var template = new DefaultServer
                {
                    PersistenceConfiguration = new ConfigureEndpointInMemoryPersistence()
                };

                EndpointSetup(template, (endpointConfiguration, _) => endpointConfiguration.UseTransport(new Core.FakeTransport.FakeTransport()));
            }
        }

        class FakeReceiver : IPushMessages
        {
            PushSettings pushSettings;

            public FakeReceiver(PushSettings pushSettings)
            {
                this.pushSettings = pushSettings;
            }

            public void Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
            {
                // The LimitMessageProcessingConcurrencyTo setting only applies to the input queue
                if (pushSettings.InputQueue == Conventions.EndpointNamingConvention(typeof(ThrottledEndpoint)))
                {
                    Assert.AreEqual(10, limitations.MaxConcurrency);
                }
            }

            public Task Stop()
            {
                return Task.FromResult(0);
            }

            public IManageSubscriptions Subscriptions { get; }
        }

        class FakeQueueCreator : ICreateQueues
        {
            public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
            {
                return Task.FromResult(0);
            }
        }

        class FakeDispatcher : IDispatchMessages
        {
            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction)
            {
                return Task.FromResult(0);
            }
        }

        class FakeTransport : TransportDefinition
        {
            public override Task<TransportInfrastructure> Initialize(TransportSettings settings)
            {
                return Task.FromResult<TransportInfrastructure>(new FakeTransportInfrastructure());
            }
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public FakeTransportInfrastructure()
            {
                Dispatcher = new FakeDispatcher();
            }

            public override bool SupportsTTBR { get; } = false;
            public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.None;

            public override EndpointAddress BuildLocalAddress(string queueName)
            {
                return new EndpointAddress(string.Empty, null, new Dictionary<string, string>(), null);
            }

            public override string ToTransportAddress(EndpointAddress logicalAddress)
            {
                return logicalAddress.ToString();
            }

            public override Task<IPushMessages> CreateReceiver(ReceiveSettings receiveSettings)
            {
                return Task.FromResult<IPushMessages>(new FakeReceiver(receiveSettings.settings));
            }
        }
    }
}
