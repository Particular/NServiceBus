namespace NServiceBus
{
    using System.Threading.Tasks;

    interface IEventAggregator
    {
        Task Raise<T>(T @event);
    }
}