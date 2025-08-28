﻿namespace NServiceBus.AcceptanceTests.Core.FakeTransport;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transport;

public class FakeTransportInfrastructure : TransportInfrastructure
{
    readonly FakeTransport.StartUpSequence startUpSequence;
    readonly HostSettings hostSettings;
    readonly ReceiveSettings[] receivers;
    readonly FakeTransport transportSettings;

    public FakeTransportInfrastructure(FakeTransport.StartUpSequence startUpSequence, HostSettings hostSettings,
        ReceiveSettings[] receivers, FakeTransport transportSettings)
    {
        this.startUpSequence = startUpSequence;
        this.hostSettings = hostSettings;
        this.receivers = receivers;
        this.transportSettings = transportSettings;
    }

    public void ConfigureReceiveInfrastructure() =>
        Receivers = receivers
            .Select(r =>
                new FakeReceiver(
                    r.Id,
                    r.ReceiveAddress.ToString(),
                    transportSettings,
                    startUpSequence,
                    hostSettings.CriticalErrorAction))
            .ToDictionary<FakeReceiver, string, IMessageReceiver>(r => r.Id, r => r);

    public void ConfigureSendInfrastructure()
    {
        Dispatcher = new FakeDispatcher();
    }

    public override string GetManifest() => string.Empty;

    public override Task Shutdown(CancellationToken cancellationToken = default)
    {
        startUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(Shutdown)}");

        if (transportSettings.ErrorOnTransportDispose != null)
        {
            throw transportSettings.ErrorOnTransportDispose;
        }

        return Task.CompletedTask;
    }

    public override string ToTransportAddress(QueueAddress queueAddress)
    {
        var address = queueAddress.BaseAddress;

        var discriminator = queueAddress.Discriminator;

        if (!string.IsNullOrEmpty(discriminator))
        {
            address += "-" + discriminator;
        }

        var qualifier = queueAddress.Qualifier;

        if (!string.IsNullOrEmpty(qualifier))
        {
            address += "-" + qualifier;
        }

        return address;
    }
}