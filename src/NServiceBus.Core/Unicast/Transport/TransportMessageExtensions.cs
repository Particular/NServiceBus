namespace NServiceBus.Unicast.Transport
{
    static class TransportMessageExtensions
    {
        public static bool IsControlMessage(this TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }
    }
}