namespace NServiceBus.Faults
{
    /// <summary>
    /// Class holding keys to message headers for faults.
    /// </summary>
    public static class HeaderKeys
    {
        /// <summary>
        /// Header key for setting/getting the queue at which the message processing failed.
        /// </summary>
        public const string FailedQ = "NServiceBus.FailedQ";

        /// <summary>
        /// Header key for setting/getting the ID of the message as it was when it failed processing.
        /// </summary>
        public const string OriginalId = "NServiceBus.OriginalId";
    }
}
