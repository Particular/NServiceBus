namespace NServiceBus.Routing
{
    using System.Threading;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Creates a new <see cref="SingleInstanceRoundRobinDistributionStrategy"/> instance.
        /// </summary>
        /// <param name="endpoint">The name of the endpoint this distribution strategy resolves instances for.</param>
        /// <param name="scope">The scope for this strategy.</param>
        public SingleInstanceRoundRobinDistributionStrategy(string endpoint, DistributionStrategyScope scope)
            : base(endpoint, scope)
        {
        }

        /// <summary>
        /// Selects a destination instance for a message from all known addresses of a logical endpoint.
        /// </summary>
        public override string SelectDestination(DistributionContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            if (context.ReceiverAddresses.Length == 0)
            {
                return default;
            }
            var i = Interlocked.Increment(ref index);
            var result = context.ReceiverAddresses[(int)(i % context.ReceiverAddresses.Length)];
            return result;
        }

        // start with -1 so the index will be at 0 after the first increment.
        long index = -1;
    }
}