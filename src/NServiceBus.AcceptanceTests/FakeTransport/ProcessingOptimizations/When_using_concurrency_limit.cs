namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;
    using Transports;

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
                EndpointSetup<DefaultServer>(c => c.UseTransport<FakeTransport>());
            }
        }

        class FakeReceiver : IPushMessages
        {
            public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError, Func<string, Exception, Task> onCriticalError, PushSettings settings)
            {
                return Task.FromResult(0);
            }

            public Task Start(PushRuntimeSettings limitations)
            {
                Assert.AreEqual(10, limitations.MaxConcurrency);

                return Task.FromResult(0);
            }

            public Task Stop()
            {
                return Task.FromResult(0);
            }
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
            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                return Task.FromResult(0);
            }
        }

        class FakeTransport : TransportDefinition
        {
            public override string ExampleConnectionStringForErrorMessage => null;

            public override bool RequiresConnectionString => false;

            public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
            {
                return new FakeTransportInfrastructure();
            }
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public override IEnumerable<Type> DeliveryConstraints { get; } = Enumerable.Empty<Type>();
            public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.None;
            public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);

            public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
            {
                return instance;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return logicalAddress.ToString();
            }

            public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
            {
                return new TransportReceiveInfrastructure(() => new FakeReceiver(), () => new FakeQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
            }

            public override TransportSendInfrastructure ConfigureSendInfrastructure()
            {
                return new TransportSendInfrastructure(() => new FakeDispatcher(), () => Task.FromResult(StartupCheckResult.Success));
            }

            public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
            {
                throw new NotImplementedException();
            }
        }
    }
}