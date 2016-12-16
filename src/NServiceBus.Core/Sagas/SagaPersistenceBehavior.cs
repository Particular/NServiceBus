namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Sagas;
    using Transport;

    class SagaPersistenceBehavior : IBehavior<IInvokeHandlerContext, IInvokeHandlerContext>
    {
        public SagaPersistenceBehavior(ISagaPersister persister, ICancelDeferredMessages timeoutCancellation, SagaMetadataCollection sagaMetadataCollection)
        {
            sagaPersister = persister;
            this.timeoutCancellation = timeoutCancellation;
            this.sagaMetadataCollection = sagaMetadataCollection;
        }

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

            currentContext = context;

            RemoveSagaHeadersIfProcessingAEvent(context);

            var saga = context.MessageHandler.Instance as Saga;

            if (saga == null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var sagaMetadata = sagaMetadataCollection.Find(context.MessageHandler.Instance.GetType());
            var sagaInstanceState = new ActiveSagaInstance(saga, sagaMetadata, () => DateTime.UtcNow);

            //so that other behaviors can access the saga
            context.Extensions.Set(sagaInstanceState);

            var loadedEntity = await TryLoadSagaEntity(sagaMetadata, context).ConfigureAwait(false);

            if (loadedEntity == null)
            {
                //if this message are not allowed to start the saga
                if (IsMessageAllowedToStartTheSaga(context, sagaMetadata))
                {
                    context.Extensions.Get<SagaInvocationResult>().SagaFound();
                    sagaInstanceState.AttachNewEntity(CreateNewSagaEntity(sagaMetadata, context));
                }
                else
                {
                    if (!context.Headers.ContainsKey(Headers.SagaId))
                    {
                        var finderDefinition = GetSagaFinder(sagaMetadata, context);
                        if (finderDefinition == null)
                        {
                            throw new Exception($"Message type {context.MessageBeingHandled.GetType().Name} is handled by saga {sagaMetadata.SagaType.Name}, but the saga does not contain a property mapping or custom saga finder to map the message to saga data. Consider adding a mapping in the saga's {nameof(Saga.ConfigureHowToFindSaga)} method.");
                        }
                    }

                    sagaInstanceState.MarkAsNotFound();

                    //we don't invoke not found handlers for timeouts
                    if (isTimeoutMessage)
                    {
                        context.Extensions.Get<SagaInvocationResult>().SagaFound();
                        logger.InfoFormat("No saga found for timeout message {0}, ignoring since the saga has been marked as complete before the timeout fired", context.MessageId);
                    }
                    else
                    {
                        context.Extensions.Get<SagaInvocationResult>().SagaNotFound();
                    }
                }
            }
            else
            {
                context.Extensions.Get<SagaInvocationResult>().SagaFound();
                sagaInstanceState.AttachExistingEntity(loadedEntity);
            }

            await next(context).ConfigureAwait(false);

            if (sagaInstanceState.NotFound)
            {
                return;
            }

            if (saga.Completed)
            {
                if (!sagaInstanceState.IsNew)
                {
                    await sagaPersister.Complete(saga.Entity, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                }

                if (saga.Entity.Id != Guid.Empty)
                {
                    await timeoutCancellation.CancelDeferredMessages(saga.Entity.Id.ToString(), context).ConfigureAwait(false);
                }

                logger.DebugFormat("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.Metadata.Name, saga.Entity.Id);

                sagaInstanceState.Completed();
            }
            else
            {
                sagaInstanceState.ValidateChanges();

                if (sagaInstanceState.IsNew)
                {
                    ActiveSagaInstance.CorrelationPropertyInfo correlationProperty;
                    var sagaCorrelationProperty = SagaCorrelationProperty.None;

                    if (sagaInstanceState.TryGetCorrelationProperty(out correlationProperty))
                    {
                        sagaCorrelationProperty = new SagaCorrelationProperty(correlationProperty.PropertyInfo.Name, correlationProperty.Value);
                    }

                    await sagaPersister.Save(saga.Entity, sagaCorrelationProperty, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                }
                else
                {
                    await sagaPersister.Update(saga.Entity, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                }

                sagaInstanceState.Updated();
            }
        }

        static void RemoveSagaHeadersIfProcessingAEvent(IInvokeHandlerContext context)
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

        static bool IsMessageAllowedToStartTheSaga(IInvokeHandlerContext context, SagaMetadata sagaMetadata)
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

            headers[Headers.IsSagaTimeoutMessage] = bool.TrueString;
            return true;
        }

        Task<IContainSagaData> TryLoadSagaEntity(SagaMetadata metadata, IInvokeHandlerContext context)
        {
            string sagaId;

            if (context.Headers.TryGetValue(Headers.SagaId, out sagaId) && !string.IsNullOrEmpty(sagaId))
            {
                var sagaEntityType = metadata.SagaEntityType;

                //since we have a saga id available we can now shortcut the finders and just load the saga
                var loaderType = typeof(LoadSagaByIdWrapper<>).MakeGenericType(sagaEntityType);

                var loader = (SagaLoader)Activator.CreateInstance(loaderType);

                return loader.Load(sagaPersister, sagaId, context.SynchronizedStorageSession, context.Extensions);
            }

            var finderDefinition = GetSagaFinder(metadata, context);

            //check if we could find a finder
            if (finderDefinition == null)
            {
                return DefaultSagaDataCompletedTask;
            }

            var finderType = finderDefinition.Type;
            var finder = (SagaFinder)currentContext.Builder.Build(finderType);

            return finder.Find(currentContext.Builder, finderDefinition, context.SynchronizedStorageSession, context.Extensions, context.MessageBeingHandled);
        }

        SagaFinderDefinition GetSagaFinder(SagaMetadata metadata, IInvokeHandlerContext context)
        {
            foreach (var messageType in context.MessageMetadata.MessageHierarchy)
            {
                SagaFinderDefinition finderDefinition;
                if (metadata.TryGetFinder(messageType.FullName, out finderDefinition))
                {
                    return finderDefinition;
                }
            }
            return null;
        }

        IContainSagaData CreateNewSagaEntity(SagaMetadata metadata, IInvokeHandlerContext context)
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

            var lookupValues = context.Extensions.GetOrCreate<SagaLookupValues>();

            SagaLookupValues.LookupValue value;
            if (lookupValues.TryGet(sagaEntityType, out value))
            {
                var propertyInfo = sagaEntityType.GetProperty(value.PropertyName);

                var convertedValue = TypeDescriptor.GetConverter(propertyInfo.PropertyType)
                    .ConvertFromInvariantString(value.PropertyValue.ToString());

                propertyInfo.SetValue(sagaEntity, convertedValue);
            }

            return sagaEntity;
        }

        IInvokeHandlerContext currentContext;
        SagaMetadataCollection sagaMetadataCollection;

        ISagaPersister sagaPersister;
        ICancelDeferredMessages timeoutCancellation;

        static Task<IContainSagaData> DefaultSagaDataCompletedTask = Task.FromResult(default(IContainSagaData));
        static ILog logger = LogManager.GetLogger<SagaPersistenceBehavior>();
    }
}