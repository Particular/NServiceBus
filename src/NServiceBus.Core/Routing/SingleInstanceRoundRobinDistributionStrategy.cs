namespace NServiceBus.Routing
{
    using System.Threading;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Selects a destination instance for a command from all known instances of a logical endpoint.
        /// </summary>
        /// /// <param name="allInstances">All known endpoint instances belonging to the same logical endpoint.</param>
        /// <returns>The endpoint instance to receive the message.</returns>
        public override EndpointInstance SelectReceiver(EndpointInstance[] allInstances)
        {
            return SelectNextElement(allInstances, ref instanceIndex);
        }

        /// <summary>
        /// Selects a subscriber address to receive an event from all known subscriber addresses of a logical endpoint.
        /// </summary>
        /// <param name="subscriberAddresses">All known subscriber addresses belonging to the same logical endpoint.</param>
        /// <returns>The subscriber address to receive the event.</returns>
        public override string SelectSubscriber(string[] subscriberAddresses)
        {
            return SelectNextElement(subscriberAddresses, ref subscriberIndex);
        }

        T SelectNextElement<T>(T[] collection, ref long index)
        {
            if (collection.Length == 0)
            {
                return default(T);
            }
            var i = Interlocked.Increment(ref index);
            var result = collection[(int)(i % collection.Length)];
            return result;
        }

        // start with -1 so the index will be at 0 after the first increment.
        long instanceIndex = -1;
        long subscriberIndex = -1;
    }
}