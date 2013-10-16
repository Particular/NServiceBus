namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains out-of-band information on the logical message.
    /// </summary>
    public interface IMessageContext
    {
        /// <summary>
        /// Returns the Id of the message.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The address of the endpoint that sent the current message being handled.
        /// </summary>
        Address ReplyToAddress { get; }

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message.
        /// </summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Returns the time at which the message was sent.
        /// </summary>
        [ObsoleteEx(Replacement = @"Headers[Headers.TimeSent]", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        DateTime TimeSent { get; }
    }
}
