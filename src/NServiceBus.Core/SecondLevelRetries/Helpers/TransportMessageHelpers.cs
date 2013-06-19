namespace NServiceBus.SecondLevelRetries.Helpers
{
    using System;
    using Transports.Msmq;

    public static class TransportMessageHelpers
    {
        public static Address GetAddressOfFaultingEndpoint(TransportMessage message)
        {
            var failedQ = GetHeader(message, Faults.FaultsHeaderKeys.FailedQ);

            if (string.IsNullOrEmpty(failedQ))
            {
                failedQ =Faults.Forwarder. MessageHelpers.GetFailedQueueFromLabel(MsmqUtilities.Convert(message));
            }

            if (string.IsNullOrEmpty(failedQ))
            {
                throw new Exception("Could not find address");
            }

            return Address.Parse(failedQ);
        }

        public static string GetHeader(TransportMessage message, string key)
        {
            return message.Headers.ContainsKey(key) ? message.Headers[key] : null;
        }

        public static bool HeaderExists(TransportMessage message, string key)
        {
            return message.Headers.ContainsKey(key);
        }

        public static void SetHeader(TransportMessage message, string key, string value)
        {
            if (message.Headers.ContainsKey(key))
            {
                message.Headers[key] = value;
            }
            else
            {
                message.Headers.Add(key, value);
            }
        }

        public static int GetNumberOfRetries(TransportMessage message)
        {
            string value;
            if (message.Headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }
    }
}
