namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using IdGeneration;
    using Logging;
    using ObjectBuilder;
    using Saga;
    using Sagas.Finders;
    using Transports;

    class SagaPersistenceBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public ISagaPersister SagaPersister { get; set; }

        public IBuilder Builder { get; set; }

        public IDeferMessages MessageDeferrer { get; set; }

        public void Invoke(BehaviorContext context)
        {
            currentContext = context;
            var messages = context.Messages;
            var loadedMessageHandlers = context.Get<LoadedMessageHandlers>();


            foreach (var message in messages)
            {
                foreach (var messageHandler in loadedMessageHandlers.GetHandlersFor(message.GetType()))
                {
                    var sagaMessageHandler = messageHandler as ISaga;

                    if(sagaMessageHandler != null)
                        LoadAndAttachSagaEntity(sagaMessageHandler,message);
                }                
            }

            Next.Invoke(context);

            foreach (var sagaInstanceState in activeSagaInstances.Instances)
            {
                var saga = sagaInstanceState.Saga;

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

                if (saga.Completed)
                    logger.Debug(string.Format("{0} {1} has completed.", saga.GetType().FullName, saga.Entity.Id));
            }
        }

        void LoadAndAttachSagaEntity(ISaga saga,object message)
        {
            var sagaType = saga.GetType();

            var sagaEntityType = Features.Sagas.GetSagaEntityTypeForSagaType(sagaType);

            var finders = GetFindersFor(message, Builder, sagaEntityType);
            IContainSagaData sagaEntity = null;

            foreach (var finder in finders)
            {
                sagaEntity = UseFinderToFindSaga(finder,message);

                if (sagaEntity != null)
                    break;
            }

            var isNew = false;
            
            if (sagaEntity == null)
            {
                sagaEntity = CreateNewSagaEntity(sagaType);
                isNew = true;
            }
            saga.Entity = sagaEntity;

            activeSagaInstances.AddInstance(new SagaInstanceContainer
            {
                Saga = saga,
                IsNew = isNew
            });

         
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


        IEnumerable<IFinder> GetFindersFor(object message, IBuilder builder,Type sagaEntityType)
        {
            var sagaId = Headers.GetMessageHeader(message, Headers.SagaId);
         
            if (sagaEntityType == null || string.IsNullOrEmpty(sagaId))
            {
                var finders = Features.Sagas.GetFindersFor(message).Select(t => builder.Build(t) as IFinder).ToList();

                if (logger.IsDebugEnabled)
                    logger.DebugFormat("The following finders:{0} was allocated to message of type {1}", string.Join(";", finders.Select(t => t.GetType().Name)), message.GetType());

                return finders;
            }

            logger.DebugFormat("Message contains a saga type and saga id. Going to use the saga id finder. Type:{0}, Id:{1}", sagaEntityType, sagaId);

            return new List<IFinder> { builder.Build(typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType)) as IFinder };
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

        ActiveSagaInstances activeSagaInstances = new ActiveSagaInstances();

        readonly ILog logger = LogManager.GetLogger(typeof(SagaPersistenceBehavior));
        BehaviorContext currentContext;
    }

    class ActiveSagaInstances
    {
        public ActiveSagaInstances()
        {
            // Tuple<IContainSagaData, bool> = (data, whetherIsIsNew)
            Instances = new List<SagaInstanceContainer>();
        }

        public List<SagaInstanceContainer> Instances { get; set; }

        public void AddInstance(SagaInstanceContainer instanceContainer)
        {
            Instances.Add(instanceContainer);
        }
    }

    class SagaInstanceContainer
    {
        public ISaga Saga{ get; set; }
        public bool IsNew{ get; set; }
    }
}