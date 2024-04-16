namespace NServiceBus;

using System;
using Transport;

class TransportAddressResolver : ITransportAddressResolver
{
    readonly TransportSeam transportSeam;
    readonly IServiceProvider serviceProvider;

    public TransportAddressResolver(TransportSeam transportSeam, IServiceProvider serviceProvider)
    {
        this.transportSeam = transportSeam;
        this.serviceProvider = serviceProvider;
    }

    public string ToTransportAddress(QueueAddress queueAddress)
        => (transportSeam.GetTransportInfrastructure(serviceProvider)
        ?? throw new Exception($"Transport address resolution is not supported before the NServiceBus transport has been started. Start the NServiceBus transport before calling `{nameof(ToTransportAddress)}`")
        ).ToTransportAddress(queueAddress);
}