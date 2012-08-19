using System;
using System.Messaging;
using NServiceBus.Unicast.Queuing.Msmq;

namespace NServiceBus.Faults.Forwarder
{
    // Copied from NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
    internal static class MessageHelpers
    {
        private static string FAILEDQUEUE = "FailedQ";

        /// <summary>
        /// For compatibility with V2.6:
        /// Gets the label of the message stripping out the failed queue.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static string GetLabelWithoutFailedQueue(Message m)
        {
            if (string.IsNullOrEmpty(m.Label))
                return string.Empty;

            if (!m.Label.Contains(FAILEDQUEUE))
                return m.Label;

            var startIndex = m.Label.IndexOf(string.Format("<{0}>", FAILEDQUEUE));
            var endIndex = m.Label.IndexOf(string.Format("</{0}>", FAILEDQUEUE));
            endIndex += FAILEDQUEUE.Length + 3;

            return m.Label.Remove(startIndex, endIndex - startIndex);
        }
        /// <summary>
        /// For compatibility with V2.6:
        /// Returns the queue whose process failed processing the given message
        /// by accessing the label of the message.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static string GetFailedQueueFromLabel(Message m)
        {
            if (m.Label == null)
                return null;

            if (!m.Label.Contains(FAILEDQUEUE))
                return null;

            var startIndex = m.Label.IndexOf(string.Format("<{0}>", FAILEDQUEUE)) + FAILEDQUEUE.Length + 2;
            var count = m.Label.IndexOf(string.Format("</{0}>", FAILEDQUEUE)) - startIndex;

            return m.Label.Substring(startIndex, count);
        }
    }

    internal static class TransportMessageHelpers
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
    }
}
