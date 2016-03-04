namespace NServiceBus
{
    using Pipeline;

    interface IForkConnector
    {
    }

    // ReSharper disable once UnusedTypeParameter
    interface IForkConnector<TForkContext> : IForkConnector
        where TForkContext : IBehaviorContext
    {
    }
}