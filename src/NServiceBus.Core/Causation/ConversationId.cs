#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Holds the conversation ID to use for the outgoing message.
/// </summary>
public class ConversationId
{
    internal string Value { get; }

    ConversationId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Uses the provided value as the conversation ID.
    /// </summary>
    public static ConversationId Custom(string customValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customValue);

        return new ConversationId(customValue);
    }

    /// <summary>
    /// Uses a default COMB Guid conversation ID.
    /// </summary>
    public static ConversationId Default => new ConversationId(CombGuid.Generate().ToString());
}