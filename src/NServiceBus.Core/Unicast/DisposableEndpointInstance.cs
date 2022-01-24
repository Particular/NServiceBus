#if NETCOREAPP
namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    class DisposableEndpointInstance : RunningEndpointInstance, IAsyncDisposable
    {
        public DisposableEndpointInstance(SettingsHolder settings, HostingComponent hostingComponent, ReceiveComponent receiveComponent, FeatureComponent featureComponent, IMessageSession messageSession, TransportInfrastructure transportInfrastructure, CancellationTokenSource stoppingTokenSource) : base(settings, hostingComponent, receiveComponent, featureComponent, messageSession, transportInfrastructure, stoppingTokenSource)
        {
        }

        ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(Stop(new CancellationToken(true)));
    }
}
#endif