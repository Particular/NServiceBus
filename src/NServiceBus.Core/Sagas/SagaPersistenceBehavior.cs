namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IdGeneration;
    using Logging;
    using MessageInterfaces;
    using Pipeline;
    using Pipeline.Behaviors;
    using Saga;
    using Finders;
    using Transports;
    using Unicast;

    class SagaPersistenceBehavior : IBehavior<MessageHandlerContext>
    {
        public ISagaPersister SagaPersister { get; set; }

        public IDeferMessages MessageDeferrer { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public void Invoke(MessageHandlerContext context, Action next)
        {
            var saga = context.MessageHandler.Instance as ISaga;

            if (saga == null)
            {
                next();
                return;
            }
            
            currentContext = context;
            physicalMessage = context.Get<TransportMessage>();
       
            //todo: remove the handler
            var sagaInstanceState = new ActiveSagaInstance(saga, context.MessageHandler, context.LogicalMessage);

            var loadedEntity = TryLoadSagaEntity(saga, sagaInstanceState.MessageToProcess);


            if (loadedEntity == null)
            {
                //if this message are not allowed to start the saga
                if (!Features.Sagas.ShouldMessageStartSaga(sagaInstanceState.SagaType,
                    sagaInstanceState.MessageToProcess.MessageType))
                {
                    sagaInstanceState.MarkAsNotFound();

                    InvokeSagaNotFoundHandlers(sagaInstanceState);
                    return;
                }

                sagaInstanceState.AttachNewEntity(CreateNewSagaEntity(sagaInstanceState.SagaType));
            }
            else
            {
                sagaInstanceState.AttachExistingEntity(loadedEntity);
            }

            if (IsTimeoutMessage(sagaInstanceState.MessageToProcess))
            {
                sagaInstanceState.Handler.Invocation = HandlerInvocationCache.InvokeTimeout;
            }

            //so that other behaviors can access the saga
            context.Set(sagaInstanceState);

            next();

            if (sagaInstanceState.NotFound)
                return;

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

                logger.Debug(string.Format("{0} {1} has completed.", saga.GetType().FullName, saga.Entity.Id));
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

        void InvokeSagaNotFoundHandlers(ActiveSagaInstance sagaInstance)
        {
            logger.InfoFormat("Could not find a saga for the message type {0} with id {1}. Going to invoke SagaNotFoundHandlers.", sagaInstance.MessageToProcess.GetType().FullName, physicalMessage.Id);

            foreach (var handler in currentContext.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler: {0}", handler.GetType().FullName);
                handler.Handle(sagaInstance.MessageToProcess);
            }
        }



        static bool IsTimeoutMessage(LogicalMessage message)
        {
            return !string.IsNullOrEmpty(Headers.GetMessageHeader(message.Instance, Headers.IsSagaTimeoutMessage));
        }

        IContainSagaData TryLoadSagaEntity(ISaga saga, LogicalMessage message)
        {
            var sagaType = saga.GetType();

            var sagaEntityType = Features.Sagas.GetSagaEntityTypeForSagaType(sagaType);

            var finders = GetFindersFor(message.MessageType, sagaEntityType);

            foreach (var finder in finders)
            {
                var sagaEntity = UseFinderToFindSaga(finder, message.Instance);

                if (sagaEntity != null)
                    return sagaEntity;
            }

            return null;
        }

        void NotifyTimeoutManagerThatSagaHasCompleted(ISaga saga)
        {
            MessageDeferrer.ClearDeferredMessages(Headers.SagaId, saga.Entity.Id.ToString());
        }

        static IContainSagaData UseFinderToFindSaga(IFinder finder, object message)
        {
            var method = Features.Sagas.GetFindByMethodForFinder(finder, message);

            if (method != null)
                return method.Invoke(finder, new[] { message }) as IContainSagaData;

            return null;
        }


        IEnumerable<IFinder> GetFindersFor(Type messageType, Type sagaEntityType)
        {
            string sagaId = null;

            physicalMessage.Headers.TryGetValue(Headers.SagaId, out sagaId);

            if (sagaEntityType == null || string.IsNullOrEmpty(sagaId))
            {
                var finders = Features.Sagas.GetFindersForMessageAndEntity(messageType, sagaEntityType).Select(t => currentContext.Builder.Build(t) as IFinder).ToList();

                if (logger.IsDebugEnabled)
                    logger.DebugFormat("The following finders:{0} was allocated to message of type {1}", string.Join(";", finders.Select(t => t.GetType().Name)), messageType);

                return finders;
            }

            logger.DebugFormat("Message contains a saga type and saga id. Going to use the saga id finder. Type:{0}, Id:{1}", sagaEntityType, sagaId);

            return new List<IFinder> { currentContext.Builder.Build(typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType)) as IFinder };
        }

        IContainSagaData CreateNewSagaEntity(Type sagaType)
        {
            var sagaEntityType = Features.Sagas.GetSagaEntityTypeForSagaType(sagaType);

            if (sagaEntityType == null)
                throw new InvalidOperationException("No saga entity type could be found for saga: " + sagaType);

            var sagaEntity = (IContainSagaData)Activator.CreateInstance(sagaEntityType);

            sagaEntity.Id = CombGuid.Generate();

            if (physicalMessage.ReplyToAddress != null)
                sagaEntity.Originator = physicalMessage.ReplyToAddress.ToString();

            sagaEntity.OriginalMessageId = physicalMessage.Id;

            return sagaEntity;
        }

        List<ActiveSagaInstance> activeSagaInstances;

        readonly ILog logger = LogManager.GetLogger(typeof(SagaPersistenceBehavior));
        MessageHandlerContext currentContext;
        TransportMessage physicalMessage;
    }
}