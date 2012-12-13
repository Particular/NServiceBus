namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// COntains transport message headers
    /// </summary>
    public static class TransportHeaderKeys
    {
        /// <summary>
        /// Return OriginalId if present. If not return Transport message Id.
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public static string GetOriginalId(this TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(Headers.OriginalId) && (!string.IsNullOrWhiteSpace(transportMessage.Headers[Headers.OriginalId])))
                return transportMessage.Headers[Headers.OriginalId];
            
            return transportMessage.Id;
        }
    }
}
