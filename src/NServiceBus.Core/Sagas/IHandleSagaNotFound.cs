#nullable enable

namespace NServiceBus;

using System.Threading.Tasks;

/// <summary>
/// Implementations will be invoked when a message arrives that should have been processed
/// by a saga, but no existing saga was found. This does not include the scenario when
/// a saga is allowed to be created for the given message type.
/// </summary>
public interface IHandleSagaNotFound
{
    /// <summary>
    /// Called when the saga the not found handler is attached to was not found as part of processing the provided message.
    /// </summary>
    Task Handle(object message, IMessageProcessingContext context);
}