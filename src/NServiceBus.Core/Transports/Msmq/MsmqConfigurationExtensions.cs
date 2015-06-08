namespace NServiceBus
{
    using System.Messaging;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// Adds extensions methods to <see cref="TransportExtensions{T}"/> for configuration purposes.
    /// </summary>
    public static class MsmqConfigurationExtensions
    {

        /// <summary>
        /// Set a delegate to use for applying the <see cref="Message.Label"/> property when sending a message.
        /// </summary>
        /// <remarks>
        /// This delagate will be used for all valid messages sent vis MSMQ.
        /// This includes not just standard messages but Audits, Errors and all control messages. 
        /// Use the <see cref="Headers.ControlMessageHeader"/> key to determin if a message is a control message.
        /// The only exception to this tull is corrupted messages that will be forwarded to the error queue with no label applied.
        /// </remarks>
        public static void ApplyLabelToMessages(this TransportExtensions<MsmqTransport> transportExtensions, MsmqLabelGenerator generateLabel)
        {
            Guard.AgainstNull(generateLabel, "generateLabel");
            transportExtensions.GetSettings()
                .Set<MsmqLabelGenerator>(generateLabel);
        }

        internal static MsmqLabelGenerator GetMessageLabelGenerator(this ReadOnlySettings settings)
        {
            MsmqLabelGenerator getMessageLabel;
            settings.TryGet(out getMessageLabel);
            return getMessageLabel;
        }
    }
}