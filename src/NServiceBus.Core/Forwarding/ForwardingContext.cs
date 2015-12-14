namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    interface ForwardingContext : BehaviorContext
    {
        OutgoingMessage Message { get; }

        string Address { get; }
    }
}