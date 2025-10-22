#nullable enable

namespace NServiceBus;

using System.Threading.Tasks;

/// <summary>
/// Implementations will be invoked when a message arrives that should have been processed
/// by a saga, but no existing saga was found. This does not include the scenario when
/// a saga will be created for the given message type.
/// </summary>
public interface ISagaNotFoundHandler
{
    /// <summary>
    /// Handler for the case when a saga was not found be a message that wasn't allowed to start the saga.
    /// </summary>
    Task Handle(object message, IMessageProcessingContext context);
}