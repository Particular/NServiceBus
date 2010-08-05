using System;

namespace NServiceBus.Grid.Messages
{
    /// <summary>
    /// Message sent to a distributor indicating that a node is ready to process another message.
    /// </summary>
    [Serializable]
    public class ReadyMessage : IMessage
    {
        /// <summary>
        /// Exposes whether or not previous ready messages from the same
        /// sender should be cleared.
        /// </summary>
        public bool ClearPreviousFromThisAddress { get; set; }
    }
}
