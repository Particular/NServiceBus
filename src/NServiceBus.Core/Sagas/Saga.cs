#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;

/// <summary>
/// This class is used to define sagas containing data and handling a message.
/// To handle more message types, implement <see cref="IHandleMessages{T}" />
/// for the relevant types.
/// To signify that the receipt of a message should start this saga,
/// implement <see cref="IAmStartedByMessages{T}" /> for the relevant message type.
/// </summary>
public abstract class Saga
{
    /// <summary>
    /// The saga's typed data.
    /// </summary>
    public IContainSagaData Entity { get; set; } = null!;

    /// <summary>
    /// Indicates that the saga is complete.
    /// In order to set this value, use the <see cref="MarkAsComplete" /> method.
    /// </summary>
    public bool Completed { get; private set; }

    /// <summary>
    /// Override this method in order to configure how this saga's data should be found.
    /// </summary>
    protected internal abstract void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration);

    /// <summary>
    /// Request for a timeout to occur at the given <see cref="DateTime" />.
    /// </summary>
    /// <param name="context">The context which is used to send the timeout.</param>
    /// <param name="at"><see cref="DateTimeOffset" /> to send timeout <typeparamref name="TTimeoutMessageType" />.</param>
    protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, DateTimeOffset at) where TTimeoutMessageType : new()
    {
        return RequestTimeout(context, at, new TTimeoutMessageType());
    }

    /// <summary>
    /// Request for a timeout to occur at the given <see cref="DateTime" />.
    /// </summary>
    /// <param name="context">The context which is used to send the timeout.</param>
    /// <param name="at"><see cref="DateTimeOffset" /> to send timeout <paramref name="timeoutMessage" />.</param>
    /// <param name="timeoutMessage">The message to send after <paramref name="at" /> is reached.</param>
    protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, DateTimeOffset at, TTimeoutMessageType timeoutMessage)
    {
        VerifySagaCanHandleTimeout(timeoutMessage);

        var options = new SendOptions();

        options.DoNotDeliverBefore(at);
        options.RouteToThisEndpoint();

        SetTimeoutHeaders(options);

        return context.Send(timeoutMessage, options);
    }

    /// <summary>
    /// Request for a timeout to occur within the give <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="context">The context which is used to send the timeout.</param>
    /// <param name="within">Given <see cref="TimeSpan" /> to delay timeout message by.</param>
    protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, TimeSpan within) where TTimeoutMessageType : new()
    {
        return RequestTimeout(context, within, new TTimeoutMessageType());
    }

    /// <summary>
    /// Request for a timeout to occur within the given <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="context">The context which is used to send the timeout.</param>
    /// <param name="within">Given <see cref="TimeSpan" /> to delay timeout message by.</param>
    /// <param name="timeoutMessage">The message to send after <paramref name="within" /> expires.</param>
    protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, TimeSpan within, TTimeoutMessageType timeoutMessage)
    {
        VerifySagaCanHandleTimeout(timeoutMessage);

        var sendOptions = new SendOptions();

        sendOptions.DelayDeliveryWith(within);
        sendOptions.RouteToThisEndpoint();

        SetTimeoutHeaders(sendOptions);

        return context.Send(timeoutMessage, sendOptions);
    }

    /// <summary>
    /// Sends the <paramref name="message" /> using the bus to the endpoint that caused this saga to start.
    /// </summary>
    protected Task ReplyToOriginator(IMessageHandlerContext context, object message, IReadOnlyDictionary<string, string>? outgoingHeaders = null)
    {
        if (string.IsNullOrEmpty(Entity.Originator))
        {
            throw new Exception("Entity.Originator cannot be null. Perhaps the sender is a SendOnly endpoint.");
        }

        var options = new ReplyOptions();

        foreach (var keyValuePair in outgoingHeaders ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            options.OutgoingHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        options.SetDestination(Entity.Originator);
        options.Context.Set(new AttachCorrelationIdBehavior.State(Entity.OriginalMessageId));

        //until we have metadata we just set this to null to avoid our own saga id being set on outgoing messages since
        //that would cause the saga that started us (if it was a saga) to not be found. When we have metadata available in the future we'll set the correct id and type
        // and get true auto correlation to work between sagas
        options.Context.Set(new PopulateAutoCorrelationHeadersForRepliesBehavior.State
        {
            SagaTypeToUse = null,
            SagaIdToUse = null
        });

        return context.Reply(message, options);
    }

    /// <summary>
    /// Marks the saga as complete.
    /// This may result in the sagas state being deleted by the persister.
    /// </summary>
    protected void MarkAsComplete()
    {
        Completed = true;
    }

    void VerifySagaCanHandleTimeout<TTimeoutMessageType>(TTimeoutMessageType timeoutMessage)
    {
        var canHandleTimeoutMessage = this is IHandleTimeouts<TTimeoutMessageType>;
        if (!canHandleTimeoutMessage)
        {
            var message = $"The type '{GetType().Name}' cannot request timeouts for '{timeoutMessage}' because it does not implement 'IHandleTimeouts<{typeof(TTimeoutMessageType).FullName}>'";
            throw new Exception(message);
        }
    }

    void SetTimeoutHeaders(ExtendableOptions options)
    {
        options.SetHeader(Headers.SagaId, Entity.Id.ToString());
        options.SetHeader(Headers.IsSagaTimeoutMessage, bool.TrueString);
        options.SetHeader(Headers.SagaType, GetType().AssemblyQualifiedName);
    }
}