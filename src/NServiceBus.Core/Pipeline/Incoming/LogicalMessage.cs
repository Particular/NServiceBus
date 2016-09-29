namespace NServiceBus.Pipeline
{
    using System;
    using Unicast.Messages;

    /// <summary>
    /// The logical message.
    /// </summary>
    public class LogicalMessage
    {
        /// <summary>
        /// Create a new <see cref="LogicalMessage"/> instance containing a message object and it's corresponding <see cref="MessageMetadata"/>.
        /// </summary>
        public LogicalMessage(MessageMetadata metadata, object message)
        {
            Instance = message;
            Metadata = metadata;
        }

        /// <summary>
        /// The <see cref="Type" /> of the message instance.
        /// </summary>
        public Type MessageType => Metadata.MessageType;


        /// <summary>
        /// Message metadata.
        /// </summary>
        public MessageMetadata Metadata { get; internal set; }

        /// <summary>
        /// The message instance.
        /// </summary>
        public object Instance { get; internal set; }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        /// <param name="newInstance">The new instance.</param>
        [ObsoleteEx(
            RemoveInVersion = "7", 
            TreatAsErrorFromVersion = "6", 
            ReplacementTypeOrMember = "IIncomingLogicalMessageContext.UpdateMessageInstance(object newInstance)")]
        public void UpdateMessageInstance(object newInstance)
        {
            throw new NotImplementedException();
        }
    }
}