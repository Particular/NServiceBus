namespace NServiceBus
{
    using System;

    /// <summary>
    /// Holds the conversation id to use for the outgoing message.
    /// </summary>
    public struct ConversationId
    {
        internal string Value { get; }

        ConversationId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Uses the provided value as the conversation id.
        /// </summary>
        /// <param name="customValue"></param>
        public static ConversationId Custom(string customValue)
        {
            if (string.IsNullOrEmpty(customValue))
            {
                throw new ArgumentException("Null or empty conversation ID's are not allowed.");
            }

            return new ConversationId(customValue);
        }

        /// <summary>
        /// Uses a default COMB Guid conversation id.
        /// </summary>
        public static ConversationId Default => new ConversationId(CombGuid.Generate().ToString());
    }
}