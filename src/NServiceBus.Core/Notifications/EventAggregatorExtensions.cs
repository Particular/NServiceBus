namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    static class EventAggregatorExtensions
    {

        //note: This is provided as an extension method to keep the notifications internal for now. Once 
        // we make them public this will be moved to the IBehaviorContext interface
        public static Task RaiseNotification<T>(this IBehaviorContext context, T notification)
        {
            return context.Extensions.Get<IEventAggregator>()
                .Raise(notification);
        }
    }
}