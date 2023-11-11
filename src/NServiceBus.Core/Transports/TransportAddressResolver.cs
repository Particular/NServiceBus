﻿namespace NServiceBus;

using Transport;

class TransportAddressResolver : ITransportAddressResolver
{
    readonly TransportInfrastructure transportInfrastructure;

    public TransportAddressResolver(TransportInfrastructure transportInfrastructure)
    {
        this.transportInfrastructure = transportInfrastructure;
    }

    public string ToTransportAddress(QueueAddress queueAddress) => transportInfrastructure.ToTransportAddress(queueAddress);
}