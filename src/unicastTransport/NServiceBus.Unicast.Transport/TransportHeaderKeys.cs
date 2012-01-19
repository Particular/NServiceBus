namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// COntains transport message headers
    /// </summary>
    public static class TransportHeaderKeys
    {
        /// <summary>
        /// Header key for setting/getting the ID of the message as it was when it failed processing.
        /// </summary>
        public const string OriginalId = "NServiceBus.OriginalId";

        /// <summary>
        /// Used for correlation id message.
        /// </summary>
        public const string IdForCorrelation = "CorrId";

        /// <summary>
        /// Return OriginalId if present. If not return Transport message Id.
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public static string GetOriginalId(this TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(OriginalId) && (!string.IsNullOrWhiteSpace(transportMessage.Headers[OriginalId])))
                return transportMessage.Headers[OriginalId];
            
            return transportMessage.Id;
        }
        /// <summary>
        /// Returns IdForCorrelation if not null, otherwise, return Transport message Id.
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public static string GetIdForCorrelation(this TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(IdForCorrelation) && (!string.IsNullOrWhiteSpace(transportMessage.Headers[IdForCorrelation])))
                return transportMessage.Headers[IdForCorrelation];

            return transportMessage.Id;
        }
    }
}
