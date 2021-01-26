namespace NServiceBus.AcceptanceTests.Core.FakeTransport.ProcessingOptimizations
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
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

            //Assert in FakeReceiver.StartReceive
        }

        class ThrottledEndpoint : EndpointConfigurationBuilder
        {
            public ThrottledEndpoint()
            {
                var template = new DefaultServer
                {
                    PersistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence()
                };

                EndpointSetup(template, (endpointConfiguration, _) => endpointConfiguration.UseTransport(new FakeTransport()));
            }
        }

        class FakeReceiver : IMessageReceiver
        {
            PushRuntimeSettings pushSettings;

            public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken)
            {
                pushSettings = limitations;
                return Task.CompletedTask;
            }

            public Task StartReceive(CancellationToken cancellationToken)
            {
                Assert.AreEqual(10, pushSettings.MaxConcurrency);

                return Task.CompletedTask;
            }

            public Task StopReceive(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public ISubscriptionManager Subscriptions { get; }

            public string Id { get; } = "Main";
        }

        class FakeDispatcher : IMessageDispatcher
        {
            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }
        }

        class FakeTransport : TransportDefinition
        {
            public FakeTransport() : base(TransportTransactionMode.None, false, false, false)
            {
            }

            public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken)
            {
                return Task.FromResult<TransportInfrastructure>(new FakeTransportInfrastructure(receivers));
            }

            public override string ToTransportAddress(QueueAddress address)
            {
                return address.ToString();
            }

            public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
            {
                return new[]
                {
                    TransportTransactionMode.None,
                    TransportTransactionMode.ReceiveOnly,
                    TransportTransactionMode.TransactionScope,
                    TransportTransactionMode.SendsAtomicWithReceive
                };
            }
        }

        sealed class FakeTransportInfrastructure : TransportInfrastructure
        {
            public FakeTransportInfrastructure(ReceiveSettings[] receiveSettings)
            {
                Dispatcher = new FakeDispatcher();
                Receivers = receiveSettings.Select(settings => new FakeReceiver()).ToList<IMessageReceiver>().AsReadOnly();
            }

            public override Task Shutdown(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
