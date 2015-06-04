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
                .Set("MsmqMessageLabelGenerator", generateLabel);
        }

        internal static Func<IReadOnlyDictionary<string, string>, string> GetMessageLabelGenerator(this ReadOnlySettings settings)
        {
            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            settings.TryGet("MsmqMessageLabelGenerator", out getMessageLabel);
            return getMessageLabel;
        } 
    }

    /// <summary>
    /// The signature of the label generator used by <see cref="MsmqConfigurationExtensions.ApplyLabelToMessages"/>.
    /// </summary>
    /// <param name="headers">The message headers of the message at the point before it is placed on the wire.</param>
    /// <returns>
    /// A <see cref="string"/> used for the <see cref="Message.Label"/> or an empty string for no label. The returned value must be at most 240 characters.
    /// </returns>
    public delegate string MsmqLabelGenerator(IReadOnlyDictionary<string,string> headers);
}