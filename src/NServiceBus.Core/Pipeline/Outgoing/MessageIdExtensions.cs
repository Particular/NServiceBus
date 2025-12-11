#nullable enable

namespace NServiceBus;

using System;
using Extensibility;

/// <summary>
/// Extensions to the outgoing pipeline.
/// </summary>
public static class MessageIdExtensions
{
    /// <param name="options">Options to extend.</param>
    extension(ExtendableOptions options)
    {
        /// <summary>
        /// Allows the user to set the message id.
        /// </summary>
        /// <param name="messageId">The message id to use.</param>
        public void SetMessageId(string messageId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

            options.UserDefinedMessageId = messageId;
        }

        /// <summary>
        /// Returns the configured message id.
        /// </summary>
        /// <returns>The message id if configured or <c>null</c>.</returns>
        public string? GetMessageId()
        {
            ArgumentNullException.ThrowIfNull(options);
            return options.UserDefinedMessageId;
        }
    }
}