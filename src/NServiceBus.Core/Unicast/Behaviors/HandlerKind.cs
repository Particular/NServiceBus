namespace NServiceBus.Unicast.Behaviors
{
    internal enum HandlerKind
    {
        Message,
        Command,
        Event,
        Timeout
    }
}