using System;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;

namespace NServiceBus.Management.Retries.Helpers
{
    public static class TransportMessageHelpers
    {
        public static Address GetReplyToAddress(TransportMessage message)
        {
            var failedQ = GetHeader(message, Faults.FaultsHeaderKeys.FailedQ);

            if (string.IsNullOrEmpty(failedQ))
            {
                failedQ = MessageHelpers.GetFailedQueueFromLabel(MsmqUtilities.Convert(message));
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
            if (message.Headers.TryGetValue(SecondLevelRetriesHeaders.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }

        public static Address GetOriginalReplyToAddressAndRemoveItFromHeaders(TransportMessage message)
        {
            var originalReplyToAddress = GetHeader(message, SecondLevelRetriesHeaders.OriginalReplyToAddress);

            if (originalReplyToAddress == null)
            {
                return message.ReplyToAddress;
            }

            SetHeader(message, SecondLevelRetriesHeaders.OriginalReplyToAddress, null);

            return Address.Parse(originalReplyToAddress);
        }
    }
}