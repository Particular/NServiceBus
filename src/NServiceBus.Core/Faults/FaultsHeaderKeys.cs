namespace NServiceBus.Faults
{
    using NServiceBus.SecondLevelRetries;

    /// <summary>
    /// Class holding keys to message headers for faults.
    /// </summary>
    public static class FaultsHeaderKeys
    {
        /// <summary>
        /// Header key for setting/getting the queue at which the message processing failed.
        /// </summary>
        public const string FailedQ = "NServiceBus.FailedQ";

        /// <summary>
        /// Header key for communicating <see cref="SecondLevelRetriesProcessor"/> the value for <see cref="FailedQ"/> header.
        /// </summary>
        public const string TemporatyFailedQueue = "NServiceBus.TemporatyFailedQueue";
    }
}