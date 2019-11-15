namespace NServiceBus
{
    using System.Threading.Tasks;

    interface INotificationSubscriptions<TEvent>
    {
        Task Raise(TEvent @event);
    }
}