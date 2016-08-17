namespace NServiceBus.Routing
{
    using System.Threading;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Selects a destination instance for a message from all known addresses of a logical endpoint.
        /// </summary>
        public override string SelectReceiver(string[] receiverAddresses)
        {
            if (receiverAddresses.Length == 0)
            {
                return default(string);
            }
            var i = Interlocked.Increment(ref index);
            var result = receiverAddresses[(int)(i % receiverAddresses.Length)];
            return result;
        }

        // start with -1 so the index will be at 0 after the first increment.
        long index = -1;
    }
}