namespace NServiceBus.Unicast.Transport
{
    using Messages;

    static class TransportMessageExtensions
    {
        public static bool IsControlMessage(this TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }


        public static bool IsControlMessage(this LogicalMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }
    }
}