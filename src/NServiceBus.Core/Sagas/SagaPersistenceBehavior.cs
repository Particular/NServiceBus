﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery.TimeoutManager;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Sagas;
    using NServiceBus.Transports;

    class SagaPersistenceBehavior : HandlingStageBehavior
    {
        ISagaPersister sagaPersister;
        ICancelDeferredMessages timeoutCancellation;
        SagaMetadataCollection sagaMetadataCollection;

        public SagaPersistenceBehavior(ISagaPersister persister, ICancelDeferredMessages timeoutCancellation, SagaMetadataCollection sagaMetadataCollection)
        {
            sagaPersister = persister;
            this.timeoutCancellation = timeoutCancellation;
            this.sagaMetadataCollection = sagaMetadataCollection;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            currentContext = context;

            RemoveSagaHeadersIfProcessingAEvent(context);

            var saga = context.MessageHandler.Instance as Saga;

            if (saga == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            var sagaMetadata = sagaMetadataCollection.Find(context.MessageHandler.Instance.GetType());
            var sagaPersistenceOptions = new SagaPersistenceOptions(sagaMetadata, context);
            var sagaInstanceState = new ActiveSagaInstance(saga, sagaMetadata);

            //so that other behaviors can access the saga
            context.Set(sagaInstanceState);

            var loadedEntity = await TryLoadSagaEntity(sagaPersistenceOptions, context).ConfigureAwait(false);

            if (loadedEntity == null)
            {
                //if this message are not allowed to start the saga
                if (IsMessageAllowedToStartTheSaga(context, sagaMetadata))
                {
                    context.Get<SagaInvocationResult>().SagaFound();
                    sagaInstanceState.AttachNewEntity(CreateNewSagaEntity(sagaMetadata, context));
                }
                else
                {
                    sagaInstanceState.MarkAsNotFound();

                    //we don't invoke not found handlers for timeouts
                    if (IsTimeoutMessage(context.Headers))
                    {
                        context.Get<SagaInvocationResult>().SagaFound();
                        logger.InfoFormat("No saga found for timeout message {0}, ignoring since the saga has been marked as complete before the timeout fired", context.MessageId);
                    }
                    else
                    {
                        context.Get<SagaInvocationResult>().SagaNotFound();
                    }
                }
            }
            else
            {
                context.Get<SagaInvocationResult>().SagaFound();
                sagaInstanceState.AttachExistingEntity(loadedEntity);
            }

            await next().ConfigureAwait(false);

            if (sagaInstanceState.NotFound)
            {
                return;
            }
            sagaInstanceState.ValidateIdHasNotChanged();

            if (saga.Completed)
            {
                if (!sagaInstanceState.IsNew)
                {
                    await sagaPersister.Complete(saga.Entity, sagaPersistenceOptions).ConfigureAwait(false);
                }

                if (saga.Entity.Id != Guid.Empty)
                {
                    await timeoutCancellation.CancelDeferredMessages(saga.Entity.Id.ToString(), context).ConfigureAwait(false);
                }

                logger.DebugFormat("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.Metadata.Name, saga.Entity.Id);
            }
            else
            {
                if (sagaInstanceState.IsNew)
                {
                    await sagaPersister.Save(saga.Entity, sagaPersistenceOptions).ConfigureAwait(false);
                }
                else
                {
                    await sagaPersister.Update(saga.Entity, sagaPersistenceOptions).ConfigureAwait(false);
                }
            }
        }

        static void RemoveSagaHeadersIfProcessingAEvent(Context context)
        {

            // We need this for backwards compatibility because in v4.0.0 we still have this headers being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
            string messageIntentString;
            if (context.Headers.TryGetValue(Headers.MessageIntent, out messageIntentString))
            {
                MessageIntentEnum messageIntent;

                if (Enum.TryParse(messageIntentString, true, out messageIntent) && messageIntent == MessageIntentEnum.Publish)
                {
                    context.Headers.Remove(Headers.SagaId);
                    context.Headers.Remove(Headers.SagaType);
                }
            }
        }

        static bool IsMessageAllowedToStartTheSaga(Context context, SagaMetadata sagaMetadata)
        {
            string sagaType;

            if (context.Headers.ContainsKey(Headers.SagaId) &&
                context.Headers.TryGetValue(Headers.SagaType, out sagaType))
            {
                //we want to move away from the assembly fully qualified name since that will break if you move sagas
                // between assemblies. We use the fullname instead which is enough to identify the saga
                if (sagaType.StartsWith(sagaMetadata.Name))
                {
                    //so now we have a saga id for this saga and if we can't find it we shouldn't start a new one
                    return false;
                }
            }

            return context.MessageMetadata.MessageHierarchy.Any(messageType => sagaMetadata.IsMessageAllowedToStartTheSaga(messageType.FullName));
        }

        static bool IsTimeoutMessage(Dictionary<string, string> headers)
        {
            string isSagaTimeout;

            if (headers.TryGetValue(Headers.IsSagaTimeoutMessage, out isSagaTimeout))
            {
                return true;
            }

            string version;

            if (!headers.TryGetValue(Headers.NServiceBusVersion, out version))
            {
                return false;
            }

            if (!version.StartsWith("3."))
            {
                return false;
            }

            string sagaId;
            if (headers.TryGetValue(Headers.SagaId, out sagaId))
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

            string expire;
            if (headers.TryGetValue(TimeoutManagerHeaders.Expire, out expire))
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

            headers[Headers.IsSagaTimeoutMessage] = Boolean.TrueString;
            return true;
        }

        Task<IContainSagaData> TryLoadSagaEntity(SagaPersistenceOptions options, Context context)
        {
            string sagaId;
            var metadata = options.Metadata;

            if (context.Headers.TryGetValue(Headers.SagaId, out sagaId) && !string.IsNullOrEmpty(sagaId))
            {
                var sagaEntityType = metadata.SagaEntityType;

                //since we have a saga id available we can now shortcut the finders and just load the saga
                var loaderType = typeof(LoadSagaByIdWrapper<>).MakeGenericType(sagaEntityType);

                var loader = (SagaLoader)Activator.CreateInstance(loaderType);

                return loader.Load(sagaPersister, sagaId, options);
            }

            SagaFinderDefinition finderDefinition = null;

            foreach (var messageType in context.MessageMetadata.MessageHierarchy)
            {
                if (metadata.TryGetFinder(messageType.FullName, out finderDefinition))
                {
                    break;
                }
            }

            //check if we could find a finder
            if (finderDefinition == null)
            {
                return Task.FromResult(default(IContainSagaData));
            }

            var finderType = finderDefinition.Type;
            var finder = (SagaFinder)currentContext.Builder.Build(finderType);

            return finder.Find(currentContext.Builder, finderDefinition, options, context.MessageBeingHandled);
        }

        IContainSagaData CreateNewSagaEntity(SagaMetadata metadata, Context context)
        {
            var sagaEntityType = metadata.SagaEntityType;

            var sagaEntity = (IContainSagaData)Activator.CreateInstance(sagaEntityType);

            sagaEntity.Id = CombGuid.Generate();
            sagaEntity.OriginalMessageId = context.MessageId;

            string replyToAddress;

            if (context.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress))
            {
                sagaEntity.Originator = replyToAddress;
            }

            return sagaEntity;
        }

        Context currentContext;

        static ILog logger = LogManager.GetLogger<SagaPersistenceBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base(WellKnownStep.InvokeSaga, typeof(SagaPersistenceBehavior), "Invokes the saga logic")
            {
                InsertBefore(WellKnownStep.InvokeHandlers);
            }
        }
    }
}