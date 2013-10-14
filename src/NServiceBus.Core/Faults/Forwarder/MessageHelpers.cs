namespace NServiceBus.Faults.Forwarder
{
    using System.Messaging;

    // Copied from NServiceBus.Tools.Management.Errors.ReturnToSourceQueue
    static class MessageHelpers
    {
        static string FAILEDQUEUE = "FailedQ";

        /// <summary>
        /// For compatibility with V2.6:
        /// Gets the label of the message stripping out the failed queue.
        /// </summary>
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

}
