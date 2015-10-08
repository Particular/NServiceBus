namespace NServiceBus.Transports
{
    /// <summary>
    /// Defines the policy for outbound routing.
    /// </summary>
    public class OutboundRoutingPolicy
    {
        readonly OutboundRoutingType sends;
        readonly OutboundRoutingType publishes;
        readonly OutboundRoutingType replies;

        /// <summary>
        /// Creates new policy object.
        /// </summary>
        /// <param name="sends">Policy for sends.</param>
        /// <param name="publishes">Policy for publishes.</param>
        /// <param name="replies">Policy for replies.</param>
        public OutboundRoutingPolicy(OutboundRoutingType sends, OutboundRoutingType publishes, OutboundRoutingType replies)
        {
            this.sends = sends;
            this.publishes = publishes;
            this.replies = replies;
        }

        /// <summary>
        /// Gets the policy for sends.
        /// </summary>
        public OutboundRoutingType Sends
        {
            get { return sends; }
        }

        /// <summary>
        /// Gets the policy for publishes.
        /// </summary>
        public OutboundRoutingType Publishes
        {
            get { return publishes; }
        }

        /// <summary>
        /// Gets the policy for replies.
        /// </summary>
        public OutboundRoutingType Replies
        {
            get { return replies; }
        }
    }
}