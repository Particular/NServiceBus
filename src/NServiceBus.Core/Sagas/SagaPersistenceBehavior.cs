namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;
    using NServiceBus.Timeout;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class SagaPersistenceBehavior : HandlingStageBehavior
    {
        public ISagaPersister SagaPersister { get; set; }

        public IDeferMessages MessageDeferrer { get; set; }

        public MessageHandlerRegistry MessageHandlerRegistry { get; set; }

        public SagaMetaModel SagaMetaModel { get; set; }

        public override void Invoke(Context context, Action next)
        {
            currentContext = context;

            RemoveSagaHeadersIfProcessingAEvent(context);

            var saga = context.MessageHandler.Instance as Saga.Saga;

            if (saga == null)
            {
                next();
                return;
            }

            var sagaMetadata = SagaMetaModel.FindByName(context.MessageHandler.Instance.GetType().FullName);
            var sagaInstanceState = new ActiveSagaInstance(saga, sagaMetadata);

            //so that other behaviors can access the saga
            context.Set(sagaInstanceState);

            var loadedEntity = TryLoadSagaEntity(sagaMetadata, context);

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

            if (IsTimeoutMessage(context.Headers))
            {
                context.MessageHandler.Invocation = MessageHandlerRegistry.InvokeTimeout;
            }

            next();

            if (sagaInstanceState.NotFound)
            {
                return;
            }
            sagaInstanceState.ValidateIdHasNotChanged();

            if (saga.Completed)
            {
                if (!sagaInstanceState.IsNew)
                {
                    SagaPersister.Complete(saga.Entity);
                }

                if (saga.Entity.Id != Guid.Empty)
                {
                    NotifyTimeoutManagerThatSagaHasCompleted(saga);
                }

                logger.DebugFormat("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.Metadata.Name, saga.Entity.Id);
            }
            else
            {
                if (sagaInstanceState.IsNew)
                {
                    SagaPersister.Save(saga.Entity);
                }
                else
                {
                    SagaPersister.Update(saga.Entity);
                }
            }
        }

        private static void RemoveSagaHeadersIfProcessingAEvent(Context context)
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

        IContainSagaData TryLoadSagaEntity(SagaMetadata metadata, Context context)
        {
            string sagaId;

            if (context.Headers.TryGetValue(Headers.SagaId, out sagaId) && !string.IsNullOrEmpty(sagaId))
            {
                var sagaEntityType = metadata.SagaEntityType;

                //since we have a saga id available we can now shortcut the finders and just load the saga
                var loaderType = typeof(LoadSagaByIdWrapper<>).MakeGenericType(sagaEntityType);

                var loader = (SagaLoader)Activator.CreateInstance(loaderType);

                return loader.Load(SagaPersister, sagaId);
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
                return null;
            }

            var finderType = finderDefinition.Type;

            var finder = currentContext.Builder.Build(finderType);

            return ((SagaFinder)finder).Find(currentContext.Builder, finderDefinition, context.MessageBeingHandled);
        }

        void NotifyTimeoutManagerThatSagaHasCompleted(Saga.Saga saga)
        {
            MessageDeferrer.ClearDeferredMessages(Headers.SagaId, saga.Entity.Id.ToString());
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