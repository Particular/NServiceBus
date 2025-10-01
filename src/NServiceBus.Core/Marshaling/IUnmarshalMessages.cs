namespace NServiceBus;

using Transport;

/// <summary>
/// Defines methods to unmarshal and validate incoming transport messages.
/// </summary>
public interface IUnmarshalMessages
{
    /// <summary>
    /// Creates an <see cref="IncomingMessage"/> from the given <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="messageContext">The context of the incoming message.</param>
    /// <returns>The created <see cref="IncomingMessage"/>.</returns>
    IncomingMessage CreateIncomingMessage(MessageContext messageContext);

    /// <summary>
    /// Validates the given <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="messageContext">The context of the incoming message.</param>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    bool IsValidMessage(MessageContext messageContext);
}