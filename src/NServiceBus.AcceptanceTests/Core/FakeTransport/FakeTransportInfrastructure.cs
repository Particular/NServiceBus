namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using Settings;
    using Transport;

    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        public FakeTransportInfrastructure(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = Enumerable.Empty<Type>();

        public override TransportTransactionMode TransactionMode
        {
            get
            {
                if (settings.TryGet("FakeTransport.SupportedTransactionMode", out TransportTransactionMode supportedTransactionMode))
                {
                    return supportedTransactionMode;
                }

                return TransportTransactionMode.TransactionScope;
            }
        }

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
            return new TransportReceiveInfrastructure(() => new FakeReceiver(settings.GetOrDefault<bool>("FakeTransport.ThrowCritical"), settings.GetOrDefault<bool>("FakeTransport.ThrowOnPumpStop"), settings.GetOrDefault<Exception>()), () => new FakeQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override async Task Stop()
        {
            await Task.Yield();

            if (settings.GetOrDefault<bool>("FakeTransport.ThrowOnInfrastructureStop"))
            {
                var exception = settings.GetOrDefault<Exception>();
                throw exception;
            }
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(() => new FakeDispatcher(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException();
        }

        ReadOnlySettings settings;
    }
}