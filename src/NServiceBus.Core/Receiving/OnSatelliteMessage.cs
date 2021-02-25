namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Transport;

    /// <summary>
    /// Processes an incoming satellite message.
    /// </summary>
    public delegate Task OnSatelliteMessage(IServiceProvider serviceProvider, MessageContext messageContext, CancellationToken cancellationToken = default);
}
