namespace NServiceBus
{
    using NServiceBus.Pipeline;

    interface IForkConnector
    {   
    }

    // ReSharper disable once UnusedTypeParameter
    interface IForkConnector<TForkContext> : IForkConnector
        where TForkContext : IBehaviorContext
    {
    }
}