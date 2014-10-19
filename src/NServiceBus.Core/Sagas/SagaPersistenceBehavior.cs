namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using NServiceBus.Sagas;
    using NServiceBus.Sagas.Finders;
    using NServiceBus.Unicast;
    using Pipeline;
    using Pipeline.Contexts;
    using Saga;
    using Timeout;
    using Transports;
    using Unicast.Messages;

    class SagaPersistenceBehavior : IBehavior<IncomingContext>
    {
        public ISagaPersister SagaPersister { get; set; }

        public IDeferMessages MessageDeferrer { get; set; }

        public IMessageHandlerRegistry MessageHandlerRegistry { get; set; }

        public SagaConfigurationCache SagaConfigurationCache { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            // We need this for backwards compatibility because in v4.0.0 we still have this headers being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
            if (context.PhysicalMessage.MessageIntent == MessageIntentEnum.Publish)
            {
                context.PhysicalMessage.Headers.Remove(Headers.SagaId);
                context.PhysicalMessage.Headers.Remove(Headers.SagaType);
            }

            var saga = context.MessageHandler.Instance as Saga.Saga;
            if (saga == null)
            {
                next();
                return;
            }

            currentContext = context;

            var sagaInstanceState = new ActiveSagaInstance(saga);

            //so that other behaviors can access the saga
            context.Set(sagaInstanceState);

            var loadedEntity = TryLoadSagaEntity(saga, context.IncomingLogicalMessage);

            if (loadedEntity == null)
            {
                //if this message are not allowed to start the saga
                if (IsAllowedToStartANewSaga(context, sagaInstanceState))
                {
                    sagaInstanceState.AttachNewEntity(CreateNewSagaEntity(sagaInstanceState.SagaType));
                }
                else
                {
                    sagaInstanceState.MarkAsNotFound();

                    InvokeSagaNotFoundHandlers(sagaInstanceState.SagaType);
                }
            }
            else
            {
                sagaInstanceState.AttachExistingEntity(loadedEntity);
            }

            if (IsTimeoutMessage(context.IncomingLogicalMessage))
            {
                context.MessageHandler.Invocation = MessageHandlerRegistry.InvokeTimeout;
            }


            next();

            if (sagaInstanceState.NotFound)
            {
                return;
            }
            sagaInstanceState.ValidateIdHasNotChanged();
            LogSaga(sagaInstanceState, context);

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

                logger.Debug(string.Format("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.SagaType.FullName, saga.Entity.Id));
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

        bool IsAllowedToStartANewSaga(IncomingContext context, ActiveSagaInstance sagaInstanceState)
        {
            string sagaType;

            if (context.IncomingLogicalMessage.Headers.ContainsKey(Headers.SagaId) &&
                context.IncomingLogicalMessage.Headers.TryGetValue(Headers.SagaType, out sagaType))
            {
                //we want to move away from the assembly fully qualified name since that will break if you move sagas
                // between assemblies. We use the fullname instead which is enough to identify the saga
                if (sagaType.StartsWith(sagaInstanceState.SagaType.FullName))
                {
                    //so now we have a saga id for this saga and if we can't find it we shouldn't start a new one
                    return false;
                }
            }

            return SagaConfigurationCache.IsAStartSagaMessage(sagaInstanceState.SagaType, context.IncomingLogicalMessage.MessageType);
        }

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.1", Message = "Enriching the headers for saga related information has been moved to the SagaAudit plugin in ServiceControl.  Add a reference to the Saga audit plugin in your endpoint to get more information.")]
        void LogSaga(ActiveSagaInstance saga, IncomingContext context)
        {

            var audit = string.Format("{0}:{1}", saga.SagaType.FullName, saga.Instance.Entity.Id);

            string header;

            if (context.IncomingLogicalMessage.Headers.TryGetValue(Headers.InvokedSagas, out header))
            {
                context.IncomingLogicalMessage.Headers[Headers.InvokedSagas] += string.Format(";{0}", audit);
            }
            else
            {
                context.IncomingLogicalMessage.Headers[Headers.InvokedSagas] = audit;
            }
        }

        void InvokeSagaNotFoundHandlers(Type sagaType)
        {
            logger.InfoFormat("Could not find a saga of type '{0}' for the message type '{1}'. Going to invoke SagaNotFoundHandlers.", sagaType.FullName, currentContext.IncomingLogicalMessage.MessageType.FullName);

            foreach (var handler in currentContext.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler: {0}", handler.GetType().FullName);
                handler.Handle(currentContext.IncomingLogicalMessage.Instance);
            }
        }

        static bool IsTimeoutMessage(LogicalMessage message)
        {
            string isSagaTimeout;

            if (message.Headers.TryGetValue(Headers.IsSagaTimeoutMessage, out isSagaTimeout))
            {
                return true;
            }

            string version;

            if (!message.Headers.TryGetValue(Headers.NServiceBusVersion, out version))
            {
                return false;
            }

            if (!version.StartsWith("3"))
            {
                return false;
            }

            string sagaId;
            if (message.Headers.TryGetValue(Headers.SagaId, out sagaId))
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
            if (message.Headers.TryGetValue(TimeoutManagerHeaders.Expire, out expire))
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

            message.Headers[Headers.IsSagaTimeoutMessage] = Boolean.TrueString;
            return true;
        }

        IContainSagaData TryLoadSagaEntity(Saga.Saga saga, LogicalMessage message)
        {
            var sagaType = saga.GetType();

            var sagaEntityType = SagaConfigurationCache.GetSagaEntityTypeForSagaType(sagaType);

            var finders = GetFindersFor(message.MessageType, sagaEntityType);

            foreach (var finder in finders)
            {
                var sagaEntity = UseFinderToFindSaga(finder, message.Instance);

                if (sagaEntity != null)
                {
                    return sagaEntity;
                }
            }

            return null;
        }

        void NotifyTimeoutManagerThatSagaHasCompleted(Saga.Saga saga)
        {
            MessageDeferrer.ClearDeferredMessages(Headers.SagaId, saga.Entity.Id.ToString());
        }

        IContainSagaData UseFinderToFindSaga(IFinder finder, object message)
        {
            var method = SagaConfigurationCache.GetFindByMethodForFinder(finder, message);

            if (method != null)
            {
                return method.Invoke(finder, new[] { message }) as IContainSagaData;
            }

            return null;
        }

        IEnumerable<IFinder> GetFindersFor(Type messageType, Type sagaEntityType)
        {
            string sagaId;


            currentContext.IncomingLogicalMessage.Headers.TryGetValue(Headers.SagaId, out sagaId);

            if (sagaEntityType == null || string.IsNullOrEmpty(sagaId))
            {
                var finders = SagaConfigurationCache.GetFindersForMessageAndEntity(messageType, sagaEntityType).Select(t => currentContext.Builder.Build(t) as IFinder).ToList();

                if (logger.IsDebugEnabled)
                {
                    logger.DebugFormat("The following finders:{0} was allocated to message of type {1}", string.Join(";", finders.Select(t => t.GetType().Name)), messageType);
                }

                return finders;
            }

            logger.DebugFormat("Message contains a saga type and saga id. Going to use the saga id finder. Type:{0}, Id:{1}", sagaEntityType, sagaId);

            return new List<IFinder> { currentContext.Builder.Build(typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType)) as IFinder };
        }

        IContainSagaData CreateNewSagaEntity(Type sagaType)
        {
            var sagaEntityType = SagaConfigurationCache.GetSagaEntityTypeForSagaType(sagaType);

            if (sagaEntityType == null)
            {
                throw new InvalidOperationException("No saga entity type could be found for saga: " + sagaType);
            }

            var sagaEntity = (IContainSagaData)Activator.CreateInstance(sagaEntityType);

            sagaEntity.Id = CombGuid.Generate();

            TransportMessage physicalMessage;

            if (currentContext.TryGet(IncomingContext.IncomingPhysicalMessageKey, out physicalMessage))
            {
                sagaEntity.OriginalMessageId = physicalMessage.Id;

                if (physicalMessage.ReplyToAddress != null)
                {
                    sagaEntity.Originator = physicalMessage.ReplyToAddress.ToString();
                }
            }

            return sagaEntity;
        }

        IncomingContext currentContext;

        static ILog logger = LogManager.GetLogger<SagaPersistenceBehavior>();

        public class SagaPersistenceRegistration : RegisterStep
        {
            public SagaPersistenceRegistration()
                : base(WellKnownStep.InvokeSaga, typeof(SagaPersistenceBehavior), "Invokes the saga logic")
            {
                InsertBefore(WellKnownStep.InvokeHandlers);
                InsertAfter("SetCurrentMessageBeingHandled");
            }
        }
    }
}