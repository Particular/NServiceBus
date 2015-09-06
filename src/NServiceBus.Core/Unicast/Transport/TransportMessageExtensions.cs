namespace NServiceBus.Unicast.Transport
{
    using System.Collections.Generic;

    static class TransportMessageExtensions
    {
        public static bool IsControlMessage(IDictionary<string, string> headers)
        {
            return headers.ContainsKey(Headers.ControlMessageHeader);
        }
    }
}