namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Transport;

    /// <summary>
    /// Processes an incoming satellite message.
    /// </summary>
    public delegate Task<MessageProcessingResult> OnSatelliteMessage(IServiceProvider serviceProvider, MessageContext messageContext);
}
