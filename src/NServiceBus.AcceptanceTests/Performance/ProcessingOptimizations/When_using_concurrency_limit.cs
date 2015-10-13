namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Extensibility;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;

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
            public void Init(Func<PushContext, Task> pipe, PushSettings settings)
            {
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
            public void CreateQueueIfNecessary(string address, string account)
            {
            }
        }

        class FakeDispatcher : IDispatchMessages
        {
            public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages, ContextBag context)
            {
                return Task.FromResult(0);
            }
        }

        class FakeTransport : TransportDefinition
        {
            protected override void ConfigureForReceiving(TransportReceivingConfigurationContext context)
            {
                context.SetMessagePumpFactory(c => new FakeReceiver());
                context.SetQueueCreatorFactory(() => new FakeQueueCreator());
            }

            protected override void ConfigureForSending(TransportSendingConfigurationContext context)
            {
                context.SetDispatcherFactory(() => new FakeDispatcher());
            }

            public override IEnumerable<Type> GetSupportedDeliveryConstraints()
            {
                yield break;
            }

            public override TransactionSupport GetTransactionSupport()
            {
                return TransactionSupport.None;
            }

            public override IManageSubscriptions GetSubscriptionManager()
            {
                throw new NotImplementedException();
            }

            public override string GetDiscriminatorForThisEndpointInstance()
            {
                return null;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return logicalAddress.ToString();
            }

            public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
            {
                return new OutboundRoutingPolicy(OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend);
            }

            public override string ExampleConnectionStringForErrorMessage => null;

            public override bool RequiresConnectionString => false;
        }
    }
}