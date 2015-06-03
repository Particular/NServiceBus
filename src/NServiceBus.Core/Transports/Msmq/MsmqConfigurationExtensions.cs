namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// Adds extensions methods to <see cref="TransportExtensions{T}"/> for configuration purposes.
    /// </summary>
    public static class MsmqConfigurationExtensions
    {

        /// <summary>
        /// Set the convention to use for applying the <see cref="Message.Label"/> property when sending a message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The input into the delegate will be a readonly copy of the message headers. 
        /// </para>
        /// <para>
        /// The delegate should return the desired message label or an empty string for no label. The returned value must be at most 240 characters.
        /// </para>
        /// <para>
        /// This convention will be used for all valid messages sent vis MSMQ.
        /// This includes not just standard messages but Audits, Errors and all control messages. 
        /// Use the <see cref="Headers.ControlMessageHeader"/> key to determin if a message is a control message.
        /// The only exception to this tull is corrupted messages that will be forwarded to the error queue with no label applied.
        /// </para>
        /// </remarks>
        public static void ApplyLabelToMessages(this TransportExtensions<MsmqTransport> transportExtensions, Func<IReadOnlyDictionary<string, string>, string> labelConvention)
        {
            Guard.AgainstNull(labelConvention, "labelConvention");
            transportExtensions.GetSettings()
                .Set("MsmqMessageLabelConvention", labelConvention);
        }

        internal static Func<IReadOnlyDictionary<string, string>, string> GetMessageLabelConvention(this ReadOnlySettings settings)
        {
            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            settings.TryGet("MsmqMessageLabelConvention", out getMessageLabel);
            return getMessageLabel;
        }
    }
}