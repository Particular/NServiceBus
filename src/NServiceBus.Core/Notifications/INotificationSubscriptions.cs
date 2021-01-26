namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    interface INotificationSubscriptions<TEvent>
    {
        Task Raise(TEvent @event, CancellationToken cancellationToken);
    }
}