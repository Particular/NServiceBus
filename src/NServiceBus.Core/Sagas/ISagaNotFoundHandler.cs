#nullable enable

namespace NServiceBus;

using System.Threading.Tasks;

/// <summary>
/// Implementations will be invoked when a message arrives that should have been processed
/// by a saga, but no existing saga was found. This does not include the scenario when
/// a saga is allowed to be created for the given message type.
/// </summary>
/// <remarks>Handlers should be registered for each saga using <see cref="SagaPropertyMapper{TSagaData}.ConfigureNotFoundHandler{TSagaNotFoundHandler}"/>
/// in the <see cref="Saga{TSagaData}.ConfigureHowToFindSaga(SagaPropertyMapper{TSagaData})"/> method.</remarks>
public interface ISagaNotFoundHandler
{
    /// <summary>
    /// Called when the saga the not found handler is attached to was not found as part of processing the provided message.
    /// </summary>
    Task Handle(object message, IMessageProcessingContext context);
}