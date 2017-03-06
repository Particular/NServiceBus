namespace NServiceBus.Faults
{
    /// <summary>
    /// Class holding keys to message headers for faults.
    /// </summary>
    public static class FaultsHeaderKeys
    {
        /// <summary>
        /// Header key for setting/getting the queue at which the message processing failed.
        /// </summary>
        public const string FailedQ = "NServiceBus.FailedQ";
    }
}