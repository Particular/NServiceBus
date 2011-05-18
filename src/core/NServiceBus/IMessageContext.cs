﻿using System;
using System.Collections.Generic;

namespace NServiceBus
{
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
        /// Returns the address of the endpoint that sent this message.
        /// </summary>
        [Obsolete("Use ReplyToAddress instead.", true)]
        string ReturnAddress { get; }

        /// <summary>
        /// The address of the endpoint that sent the current message being handled.
        /// </summary>
        Address ReplyToAddress { get; }

        /// <summary>
        /// Returns the time at which the message was sent.
        /// </summary>
        DateTime TimeSent { get; }

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message.
        /// </summary>
        IDictionary<string, string> Headers { get; }
    }
}
