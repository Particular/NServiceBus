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

    public string ToTransportAddress(QueueAddress queueAddress) => transportSeam.GetTransportInfrastructure(serviceProvider).ToTransportAddress(queueAddress);
}