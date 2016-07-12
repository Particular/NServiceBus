namespace NServiceBus.Transport
{
    /// <summary>
    /// Defines the policy for outbound routing.
    /// </summary>
    public class OutboundRoutingPolicy
    {
        /// <summary>
        /// Creates new policy object.
        /// </summary>
        /// <param name="sends">Policy for sends.</param>
        /// <param name="publishes">Policy for publishes.</param>
        /// <param name="replies">Policy for replies.</param>
        public OutboundRoutingPolicy(OutboundRoutingType sends, OutboundRoutingType publishes, OutboundRoutingType replies)
        {
            Sends = sends;
            Publishes = publishes;
            Replies = replies;
        }

        /// <summary>
        /// Gets the policy for sends.
        /// </summary>
        public OutboundRoutingType Sends { get; }

        /// <summary>
        /// Gets the policy for publishes.
        /// </summary>
        public OutboundRoutingType Publishes { get; }

        /// <summary>
        /// Gets the policy for replies.
        /// </summary>
        public OutboundRoutingType Replies { get; }
    }
}