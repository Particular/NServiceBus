namespace NServiceBus.Unicast.Transport
{
    using System.Collections.Generic;

    static class TransportMessageExtensions
    {
        public static bool IsControlMessage(Dictionary<string, string> headers)
        {
            return headers.ContainsKey(Headers.ControlMessageHeader);
        }
    }
}