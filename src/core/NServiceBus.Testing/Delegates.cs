namespace NServiceBus.Testing
{
    public delegate bool PublishPredicate<T>(T message) where T : IMessage;

    public delegate bool SendPredicate<T>(T message) where T : IMessage;

    public delegate bool SendToDestinationPredicate<T>(string destination, T message) where T : IMessage;

    public delegate bool BusPublishDelegate<T>(T[] msgs) where T : IMessage;

    public delegate bool BusSendWithDestinationDelegate(string destination, IMessage[] msgs);

    public delegate bool BusSendDelegate(IMessage[] msgs);

    public delegate void HandleMessageDelegate();
}
