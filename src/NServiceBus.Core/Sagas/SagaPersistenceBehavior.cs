namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IdGeneration;
    using Logging;
    using Pipeline;
    using Pipeline.Behaviors;
    using Saga;
    using Finders;
    using Transports;
    using Unicast;

    class SagaPersistenceBehavior : IBehavior
    {
        public ISagaPersister SagaPersister { get; set; }

        public IDeferMessages MessageDeferrer { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            currentContext = context;

            activeSagaInstances = DetectSagas(context).ToList();

            foreach (var sagaInstanceState in activeSagaInstances)
            {
                var saga = sagaInstanceState.Instance;

                var loadedEntity = TryLoadSagaEntity(saga, sagaInstanceState.MessageToProcess);

                
                if (loadedEntity == null)
                {
                    //if this message are not allowed to start the saga
                    if (!Features.Sagas.ShouldMessageStartSaga(sagaInstanceState.SagaType,
                        sagaInstanceState.MessageToProcess))
                    {
                        sagaInstanceState.MarkAsNotFound();

                        InvokeSagaNotFoundHandlers(sagaInstanceState);
                        continue;
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
            }

            //so that other behaviors can access the sagas
            context.Set(new ActiveSagaInstances(activeSagaInstances));

            next();

            foreach (var sagaInstanceState in activeSagaInstances)
            {
                if (sagaInstanceState.NotFound)
                    continue;

                var saga = sagaInstanceState.Instance;

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
        }

        void InvokeSagaNotFoundHandlers(ActiveSagaInstance sagaInstance)
        {
            logger.InfoFormat("Could not find a saga for the message type {0} with id {1}. Going to invoke SagaNotFoundHandlers.", sagaInstance.MessageToProcess.GetType().FullName, currentContext.TransportMessage.Id);

            foreach (var handler in currentContext.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler: {0}",handler.GetType().FullName);
                handler.Handle(sagaInstance.MessageToProcess);
            }
        }

        IEnumerable<ActiveSagaInstance> DetectSagas(BehaviorContext context)
        {
            var loadedMessageHandlers = context.Get<LoadedMessageHandlers>();

            foreach (var message in context.Messages)
            {
                foreach (var messageHandler in loadedMessageHandlers.GetHandlersFor(message.GetType()))
                {
                    var saga = messageHandler.Instance as ISaga;

                    if (saga != null)
                    {
                        yield return new ActiveSagaInstance(saga, messageHandler, message);
                    }
                }
            }
        }

        static bool IsTimeoutMessage(object message)
        {
            return !string.IsNullOrEmpty(Headers.GetMessageHeader(message, Headers.IsSagaTimeoutMessage));
        }

        IContainSagaData TryLoadSagaEntity(ISaga saga, object message)
        {
            var sagaType = saga.GetType();

            var sagaEntityType = Features.Sagas.GetSagaEntityTypeForSagaType(sagaType);

            var finders = GetFindersFor(message.GetType(), sagaEntityType);
           
            foreach (var finder in finders)
            {
                var sagaEntity = UseFinderToFindSaga(finder, message);

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

            currentContext.TransportMessage.Headers.TryGetValue(Headers.SagaId, out sagaId);

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

            if (currentContext.TransportMessage.ReplyToAddress != null)
                sagaEntity.Originator = currentContext.TransportMessage.ReplyToAddress.ToString();

            sagaEntity.OriginalMessageId = currentContext.TransportMessage.Id;

            return sagaEntity;
        }

        List<ActiveSagaInstance> activeSagaInstances;

        readonly ILog logger = LogManager.GetLogger(typeof(SagaPersistenceBehavior));
        BehaviorContext currentContext;
    }
}