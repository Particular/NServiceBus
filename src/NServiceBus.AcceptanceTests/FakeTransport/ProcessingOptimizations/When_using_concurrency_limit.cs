namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using CriticalError = NServiceBus.CriticalError;

    public class When_using_concurrency_limit : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_pass_it_to_the_transport()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ThrottledEndpoint>(b => b.CustomConfig(c => c.LimitMessageProcessingConcurrencyTo(10)))
                .Done(c => c.EndpointsStarted)
                .Run();

            //Assert in FakeReceiver.Start
        }

        public class Context : ScenarioContext
        {
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
            public Task Init(Func<PushContext, Task> pipe, CriticalError criticalError, PushSettings settings)
            {
                return Task.FromResult(0);
            }

            public void Start(PushRuntimeSettings limitations)
            {
                Assert.AreEqual(10, limitations.MaxConcurrency);
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
            protected override TransportInfrastructure Initialize(SettingsHolder settings)
            {
                return new FakeTransportInfrastructure(
                    Enumerable.Empty<Type>(),
                    TransportTransactionMode.None,
                    new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast),
                    s => new TransportSendInfrastructure(() => new FakeDispatcher(), () => Task.FromResult(StartupCheckResult.Success)),
                    s => new TransportReceiveInfrastructure(() => new FakeReceiver(), () => new FakeQueueCreator(), () => Task.FromResult(StartupCheckResult.Success)));
            }

            
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public FakeTransportInfrastructure(IEnumerable<Type> deliveryConstraints, TransportTransactionMode transactionMode, OutboundRoutingPolicy outboundRoutingPolicy, Func<string, TransportSendInfrastructure> configureSendInfrastructure, Func<string, TransportReceiveInfrastructure> configureReceiveInfrastructure = null, Func<TransportSubscriptionInfrastructure> configureSubscriptionInfrastructure = null) : base(deliveryConstraints, transactionMode, outboundRoutingPolicy, configureSendInfrastructure, configureReceiveInfrastructure, configureSubscriptionInfrastructure)
            {
            }

            public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance, ReadOnlySettings settings)
            {
                return instance;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return logicalAddress.ToString();
            }

            public override string ExampleConnectionStringForErrorMessage => null;

            public override bool RequiresConnectionString => false;
        }
    }
}