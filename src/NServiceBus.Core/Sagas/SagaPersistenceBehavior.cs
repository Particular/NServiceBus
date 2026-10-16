namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using Pipeline;
using Sagas;
using Unicast.Messages;

class SagaPersistenceBehavior(ISagaPersister persister, ISagaIdGenerator sagaIdGenerator, SagaMetadataCollection sagaMetadataCollection, TimeoutSerializer timeoutSerializer, MessageMetadataRegistry messageMetadataRegistry, SagaTimeoutPlatformNotifier platformNotifier, IServiceProvider serviceProvider)
    : IBehavior<IInvokeHandlerContext, IInvokeHandlerContext>
{
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort saga type resolution from saga headers; when trimming removes the type, processing falls back to querying the persister for the current saga type.")]
    public async Task Invoke(IInvokeHandlerContext context, Func<IInvokeHandlerContext, Task> next)
    {
        var isTimeoutMessage = IsTimeoutMessage(context.Headers);
        var isTimeoutHandler = context.MessageHandler.IsTimeoutHandler;

        if (isTimeoutHandler && !isTimeoutMessage)
        {
            return;
        }

        if (!isTimeoutHandler && isTimeoutMessage)
        {
            return;
        }

        RemoveSagaHeadersIfProcessingAEvent(context);

        if (context.MessageHandler.Instance is not Saga saga)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // A timeout without the header was dispatched without timeout management and is fired as is
        var hasManagedTimeoutId = context.Headers.TryGetValue(SagaTimeouts.TimeoutIdHeader, out var managedTimeoutId) && isTimeoutMessage;

        if (hasManagedTimeoutId)
        {
            if (string.IsNullOrWhiteSpace(managedTimeoutId))
            {
                throw new Exception($"The saga timeout message '{context.MessageId}' has an empty '{SagaTimeouts.TimeoutIdHeader}' header.");
            }

            if (!supportsTimeoutManagement)
            {
                throw new Exception($"Received saga timeout '{managedTimeoutId}', but the configured saga persister doesn't support timeout management.");
            }

            if (context.MessageHandler.TimeoutMessageType != context.MessageMetadata.MessageType)
            {
                // Another timeout handler of the saga handles this timeout type; return before loading the saga so only one handler opens it
                return;
            }
        }

        var currentSagaMetadata = sagaMetadataCollection.Find(context.MessageHandler.Instance.GetType());

        if (context.Headers.TryGetValue(Headers.SagaType, out var targetSagaTypeString) && context.Headers.TryGetValue(Headers.SagaId, out var targetSagaId))
        {
            var targetSagaType = Type.GetType(targetSagaTypeString, false);

            if (targetSagaType == null)
            {
                logger.Warn($"Saga headers indicated that the message was intended for {targetSagaTypeString} but that type isn't available. Will fallback to query persister for a saga instance of type {currentSagaMetadata.SagaType.FullName} and saga id {targetSagaId} instead");
            }
            else
            {
                if (!sagaMetadataCollection.TryFind(targetSagaType, out var targetSagaMetaData))
                {
                    logger.Warn($"Saga headers indicated that the message was intended for {targetSagaType.FullName} but no metadata was found for that saga type. Will fallback to query persister for a saga instance of type {currentSagaMetadata.SagaType.FullName} and saga id {targetSagaId} instead");
                }
                else
                {
                    if (targetSagaMetaData.SagaType != currentSagaMetadata.SagaType)
                    {
                        //Message was intended for a different saga so no need to continue with this invocation
                        return;
                    }
                }
            }
        }

        var sagaInstanceState = new ActiveSagaInstance(saga, currentSagaMetadata, () => DateTimeOffset.UtcNow);

        //so that other behaviors can access the saga
        context.Extensions.Set(sagaInstanceState);

        var loadedEntity = await TryLoadSagaEntity(currentSagaMetadata, context).ConfigureAwait(false);

        if (loadedEntity == null)
        {
            if (IsMessageAllowedToStartTheSaga(context, currentSagaMetadata))
            {
                sagaInstanceState.AttachNewEntity(CreateNewSagaEntity(currentSagaMetadata, context));
                AttachTimeouts(saga, sagaInstanceState, context);
            }
            else
            {
                if (!context.Headers.ContainsKey(Headers.SagaId))
                {
                    _ = GetSagaFinder(currentSagaMetadata, context) ?? throw new Exception($"Message type {context.MessageBeingHandled.GetType().Name} is handled by saga {currentSagaMetadata.SagaType.Name}, but the saga does not contain a property mapping or custom saga finder to map the message to saga data. Consider adding a mapping in the saga's {nameof(Saga.ConfigureHowToFindSaga)} method.");
                }

                sagaInstanceState.MarkAsNotFound();
            }
        }
        else
        {
            sagaInstanceState.AttachExistingEntity(loadedEntity);
            AttachTimeouts(saga, sagaInstanceState, context);
        }

        if (hasManagedTimeoutId && !sagaInstanceState.NotFound && !TryStartFiring(managedTimeoutId!, sagaInstanceState, context))
        {
            return;
        }

        Activity.Current?.AddTag(ActivityTags.HandlerSagaId, sagaInstanceState.SagaId);

        await next(context).ConfigureAwait(false);

        if (sagaInstanceState.NotFound)
        {
            //we don't invoke not found handlers for timeouts
            if (isTimeoutMessage)
            {
                logger.InfoFormat("No saga found for timeout message {0}, ignoring since the saga has been marked as complete before the timeout fired", context.MessageId);
            }
            else
            {
                logger.InfoFormat("Could not find a started saga of '{0}' for message type '{1}'.", currentSagaMetadata.SagaType.FullName, context.MessageBeingHandled.GetType().FullName);

                await currentSagaMetadata.NotFoundHandler.Invoke(serviceProvider, context.MessageBeingHandled, context).ConfigureAwait(false);
            }

            return;
        }

        if (saga.Completed)
        {
            if (!sagaInstanceState.IsNew)
            {
                await persister.Complete(saga.Entity, context.SynchronizedStorageSession, context.Extensions, context.CancellationToken).ConfigureAwait(false);
            }

            logger.DebugFormat("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.Metadata.Name, saga.Entity.Id);

            await NotifyPlatform(sagaInstanceState, context).ConfigureAwait(false);

            sagaInstanceState.Completed();
        }
        else
        {
            sagaInstanceState.ValidateChanges();

            if (sagaInstanceState.IsNew)
            {
                var sagaCorrelationProperty = SagaCorrelationProperty.None;

                if (currentSagaMetadata.TryGetCorrelationProperty(out var propertyMetadata))
                {
                    sagaCorrelationProperty = new SagaCorrelationProperty(propertyMetadata.Name, propertyMetadata.Accessor.AccessFrom(sagaInstanceState.Instance.Entity));
                }

                await persister.Save(saga.Entity, sagaCorrelationProperty, context.SynchronizedStorageSession, context.Extensions, context.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                await persister.Update(saga.Entity, context.SynchronizedStorageSession, context.Extensions, context.CancellationToken).ConfigureAwait(false);
            }

            await DispatchTimeouts(saga, sagaInstanceState, context).ConfigureAwait(false);
            await NotifyPlatform(sagaInstanceState, context).ConfigureAwait(false);

            sagaInstanceState.Updated();
        }
    }

    void AttachTimeouts(Saga saga, ActiveSagaInstance sagaInstanceState, IInvokeHandlerContext context)
    {
        if (!supportsTimeoutManagement)
        {
            return;
        }

        var storedTimeouts = context.Extensions.GetOrCreateSagaTimeouts(saga.Entity);
        var timeouts = new SagaTimeouts(storedTimeouts, timeoutSerializer, messageMetadataRegistry);
        sagaInstanceState.Timeouts = timeouts;
        saga.Timeouts = timeouts;
    }

    static bool TryStartFiring(string timeoutId, ActiveSagaInstance sagaInstanceState, IInvokeHandlerContext context)
    {
        var stored = sagaInstanceState.Timeouts.Get(timeoutId);

        if (stored is null)
        {
            logger.DebugFormat("Ignoring message {0} for saga timeout {1} because the timeout already fired or was canceled.", context.MessageId, timeoutId);
            return false;
        }

        // The message ID of the dispatched timeout is the trigger ID, so a rescheduled timeout makes earlier messages stale
        if (stored.TriggerId != context.MessageId)
        {
            logger.DebugFormat("Ignoring message {0} for saga timeout {1} because the timeout was rescheduled.", context.MessageId, timeoutId);
            return false;
        }

        if (context.MessageHandler.TimeoutMessageType is not { } handledType || stored.MessageType != SagaTimeouts.MessageTypeName(handledType))
        {
            // Another timeout handler of the saga handles this timeout type
            return false;
        }

        // Removal is saved with the handler's saga changes, so a failing handler leaves the timeout in place for retries
        _ = sagaInstanceState.Timeouts.Remove(timeoutId);
        sagaInstanceState.FiringTimeout = stored;
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Timeout payload types are the annotated generic arguments of RequestTimeout and FindTimeouts, or message types registered in the message metadata registry.")]
    static async Task DispatchTimeouts(Saga saga, ActiveSagaInstance sagaInstanceState, IInvokeHandlerContext context)
    {
        if (sagaInstanceState.Timeouts is null)
        {
            return;
        }

        foreach (var (stored, payloadType) in sagaInstanceState.Timeouts.PendingTimeouts)
        {
            var options = new SendOptions();
            options.DoNotDeliverBefore(stored.DueTime);
            options.RouteToThisEndpoint();
            options.SetMessageId(stored.TriggerId);
            options.SetHeader(SagaTimeouts.TimeoutIdHeader, stored.TimeoutId);
            saga.SetTimeoutHeaders(options);

            // The stored snapshot is sent so payload changes only apply when rescheduled
            await context.Send(sagaInstanceState.Timeouts.Deserialize(stored, payloadType), payloadType, options).ConfigureAwait(false);
        }
    }

    async Task NotifyPlatform(ActiveSagaInstance sagaInstanceState, IInvokeHandlerContext context)
    {
        if (platformNotifier is null || sagaInstanceState.Timeouts is null)
        {
            return;
        }

        if (sagaInstanceState.FiringTimeout is not null)
        {
            await platformNotifier.NotifyTriggered(sagaInstanceState, sagaInstanceState.FiringTimeout, context).ConfigureAwait(false);
        }

        if (!sagaInstanceState.Instance.Completed)
        {
            await platformNotifier.NotifyChanges(sagaInstanceState, context).ConfigureAwait(false);
        }
        // Timeouts requested by a saga that completes in the same message were never reported
        else if (!sagaInstanceState.IsNew && sagaInstanceState.Timeouts.HasStoredTimeouts)
        {
            await platformNotifier.NotifySagaCompleted(sagaInstanceState, context).ConfigureAwait(false);
        }
    }

    readonly bool supportsTimeoutManagement = persister is ISupportTimeoutManagement;

    static void RemoveSagaHeadersIfProcessingAEvent(IInvokeHandlerContext context)
    {
        // We need this for backwards compatibility because in v4.0.0 we still have this header being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
        if (context.Headers.TryGetValue(Headers.MessageIntent, out var messageIntentString))
        {
            if (Enum.TryParse(messageIntentString, true, out MessageIntent messageIntent) && messageIntent == MessageIntent.Publish)
            {
                context.Headers.Remove(Headers.SagaId);
                context.Headers.Remove(Headers.SagaType);
            }
        }
    }

    static bool IsMessageAllowedToStartTheSaga(IInvokeHandlerContext context, SagaMetadata sagaMetadata)
    {
        if (context.Headers.ContainsKey(Headers.SagaId) &&
            context.Headers.TryGetValue(Headers.SagaType, out var sagaType))
        {
            //we want to move away from the assembly fully qualified name since that will break if you move sagas
            //between assemblies. We use the FullName instead, which is enough to identify the saga.
            if (sagaType.StartsWith(sagaMetadata.Name))
            {
                //so now we have a saga id for this saga, and if we can't find it, we shouldn't start a new one
                return false;
            }
        }

        return context.MessageMetadata.MessageHierarchy.Any(messageType => sagaMetadata.IsMessageAllowedToStartTheSaga(messageType.FullName!));
    }

    static bool IsTimeoutMessage(Dictionary<string, string> headers)
    {
        if (headers.TryGetValue(Headers.IsSagaTimeoutMessage, out _))
        {
            return true;
        }

        if (!headers.TryGetValue(Headers.NServiceBusVersion, out var version))
        {
            return false;
        }

        if (!version.StartsWith("3."))
        {
            return false;
        }

        if (headers.TryGetValue(Headers.SagaId, out var sagaId))
        {
            if (string.IsNullOrEmpty(sagaId))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        if (headers.TryGetValue("NServiceBus.Timeout.Expire", out var expire))
        {
            if (string.IsNullOrEmpty(expire))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        headers[Headers.IsSagaTimeoutMessage] = bool.TrueString;
        return true;
    }

    Task<IContainSagaData> TryLoadSagaEntity(SagaMetadata metadata, IInvokeHandlerContext context)
    {
        if (context.Headers.TryGetValue(Headers.SagaId, out var sagaId) && !string.IsNullOrEmpty(sagaId))
        {
            return metadata.Loader.Load(persister, sagaId, context.SynchronizedStorageSession, context.Extensions, context.CancellationToken);
        }

        var finderDefinition = GetSagaFinder(metadata, context);

        //check if we could find a finder
        if (finderDefinition == null)
        {
            return DefaultSagaDataCompletedTask;
        }

        return finderDefinition.SagaFinder.Find(context.Builder, context.SynchronizedStorageSession, context.Extensions, context.MessageBeingHandled, context.MessageHeaders, context.CancellationToken);
    }

    static SagaFinderDefinition GetSagaFinder(SagaMetadata metadata, IInvokeHandlerContext context)
    {
        foreach (var messageType in context.MessageMetadata.MessageHierarchy)
        {
            if (metadata.TryGetFinder(messageType.FullName, out var finderDefinition))
            {
                return finderDefinition;
            }
        }

        return null;
    }

    IContainSagaData CreateNewSagaEntity(SagaMetadata metadata, IInvokeHandlerContext context)
    {
        var sagaEntityType = metadata.SagaEntityType;

        var sagaEntity = metadata.SagaDataFactory();

        sagaEntity.OriginalMessageId = context.MessageId;

        if (context.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress))
        {
            sagaEntity.Originator = replyToAddress;
        }
        var lookupValues = context.Extensions.GetOrCreate<SagaLookupValues>();

        SagaCorrelationProperty correlationProperty;

        if (lookupValues.TryGet(sagaEntityType, out var value) && metadata.TryGetCorrelationProperty(out var propertyMetadata))
        {
            propertyMetadata.Accessor.WriteTo(sagaEntity, value);

            correlationProperty = new SagaCorrelationProperty(propertyMetadata.Name, value);
        }
        else
        {
            correlationProperty = SagaCorrelationProperty.None;
        }

        var sagaIdGeneratorContext = new SagaIdGeneratorContext(correlationProperty, metadata, context.Extensions);

        sagaEntity.Id = sagaIdGenerator.Generate(sagaIdGeneratorContext);

        return sagaEntity;
    }

    static readonly Task<IContainSagaData> DefaultSagaDataCompletedTask = Task.FromResult(default(IContainSagaData));
    static readonly ILog logger = LogManager.GetLogger<SagaPersistenceBehavior>();
}