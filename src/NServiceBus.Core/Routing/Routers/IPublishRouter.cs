namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    /// <summary>
    /// Routes an event to its subscribers.
    /// </summary>
    public interface IPublishRouter
    {
        /// <summary>
        /// Returns the routing strategies, determining where to send a published message.
        /// </summary>
        Task<RoutingStrategy[]> GetRoutingStrategies(IOutgoingPublishContext context);
    }
}