namespace NServiceBus.Core.Tests.Config;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NUnit.Framework;
using Transport;

public class When_configuring_transport_twice
{
    [Test] // HINT: The Azure Functions libraries depend on this functionality
    public async Task Last_one_wins()
    {
        var config = new EndpointConfiguration("Endpoint");
        config.AssemblyScanner().ExcludeAssemblies("NServiceBus.Core.Tests.dll");
        config.UseSerialization<SystemJsonSerializer>();
        var transport1 = new FakeTransportDefinition();
        var transport2 = new FakeTransportDefinition();
        config.UseTransport(transport1).DisablePublishing();
        config.UseTransport(transport2).DisablePublishing();

        var endpoint = await Endpoint.Start(config);

        await endpoint.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(transport1.Initialized, Is.False, "First transport should not be initialized");
            Assert.That(transport2.Initialized, Is.True, "Second transport should be initialized");
        });
    }

    class FakeTransportDefinition : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public bool Initialized { get; private set; }


        public FakeTransportDefinition()
            : base(TransportTransactionMode.ReceiveOnly, true, false, false)
        {
        }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            Initialized = true;

            return Task.FromResult<TransportInfrastructure>(new FakeTransportInfrastructure(receivers));
        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public FakeTransportInfrastructure(ReceiveSettings[] receivers)
            {
                Receivers = receivers.Select(x => new FakeReceiver(x)).ToDictionary(x => x.Id, x => (IMessageReceiver)x);
                Dispatcher = new FakeMessageDispatcher();
            }

            class FakeMessageDispatcher : IMessageDispatcher
            {
                public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction,
                    CancellationToken cancellationToken = default) =>
                    Task.CompletedTask;
            }

            class FakeReceiver : IMessageReceiver
            {
                public FakeReceiver(ReceiveSettings receiveSettings)
                {
                    ReceiveAddress = receiveSettings.ReceiveAddress.BaseAddress;
                    Id = receiveSettings.Id;
                }

                public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
                    CancellationToken cancellationToken = default) => Task.CompletedTask;

                public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

                public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) => Task.CompletedTask;

                public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

                public ISubscriptionManager Subscriptions { get; }
                public string Id { get; }
                public string ReceiveAddress { get; }
            }


            public override Task Shutdown(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public override string ToTransportAddress(QueueAddress address) => address.BaseAddress;
            public override string GetManifest() => string.Empty;
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => new[]
        {
            TransportTransactionMode.ReceiveOnly
        };
    }
}