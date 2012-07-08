namespace NServiceBus.Sagas.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Common.Logging;
    using Finders;
    using ObjectBuilder;
    using Saga;
    using Unicast;

    /// <summary>
    /// Dispatch factory that can dispatch messages to sagas
    /// </summary>
    public class SagaDispatcherFactory : IMessageDispatcherFactory
    {
        /// <summary>
        /// Get Dispatcher
        /// </summary>
        /// <param name="messageHandlerType">Type of the message Handler</param>
        /// <param name="builder">Builder</param>
        /// <param name="message">Message</param>
        /// <returns>Saga Dispatcher</returns>
        public IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object message)
        {
            var isTimeoutMessage = message.IsTimeoutMessage();

            if (isTimeoutMessage && !message.TimeoutHasExpired())
            {
                yield return () => Bus.HandleCurrentMessageLater();
                yield break;
            }

            var entitiesHandled = new List<ISagaEntity>();
            var sagaTypesHandled = new List<Type>();

            foreach (var finder in GetFindersFor(message, builder))
            {
                bool sagaEntityIsPersistent = true;
                ISagaEntity sagaEntity = UseFinderToFindSaga(finder, message);
                Type sagaType;

                if (sagaEntity == null)
                {
                    sagaType = Configure.GetSagaTypeToStartIfMessageNotFoundByFinder(message, finder);
                    if (sagaType == null)
                        continue;

                    if (sagaTypesHandled.Contains(sagaType))
                        continue; // don't create the same saga type twice for the same message

                    sagaEntity = CreateNewSagaEntity(sagaType);

                    sagaEntityIsPersistent = false;
                }
                else
                {
                    if (entitiesHandled.Contains(sagaEntity))
                        continue; // don't call the same saga twice

                    sagaType = Configure.GetSagaTypeForSagaEntityType(sagaEntity.GetType());
                }

                if (messageHandlerType.IsAssignableFrom(sagaType))
                    yield return () =>
                                     {
                                         var saga = (ISaga)builder.Build(sagaType);

                                         saga.Entity = sagaEntity;

                                         try
                                         {
                                             SagaContext.Current = saga;

                                             if (isTimeoutMessage && !(message is TimeoutMessage))
                                                 HandlerInvocationCache.Invoke(typeof(IHandleTimeouts<>),saga, message);
                                             else
                                                 HandlerInvocationCache.Invoke(typeof(IMessageHandler<>),saga, message);

                                             if (!saga.Completed)
                                             {
                                                 if (!sagaEntityIsPersistent)
                                                     Persister.Save(saga.Entity);
                                                 else
                                                     Persister.Update(saga.Entity);
                                             }
                                             else
                                             {
                                                 if (sagaEntityIsPersistent)
                                                     Persister.Complete(saga.Entity);

                                                 NotifyTimeoutManagerThatSagaHasCompleted(saga);
                                             }

                                             LogIfSagaIsFinished(saga);

                                         }
                                         finally
                                         {
                                             SagaContext.Current = null;
                                         }

                                     };
                sagaTypesHandled.Add(sagaType);
                entitiesHandled.Add(sagaEntity);
            }

            if (entitiesHandled.Count == 0)
                yield return () =>
                                 {
                                     logger.InfoFormat("Could not find a saga for the message type {0} with id {1}. Going to invoke SagaNotFoundHandlers.", message.GetType().FullName, Bus.CurrentMessageContext.Id);
                                     foreach (var handler in builder.BuildAll<IHandleSagaNotFound>())
                                     {
                                         logger.DebugFormat("Invoking SagaNotFoundHandler: {0}",
                                                            handler.GetType().FullName);
                                         handler.Handle(message);
                                     }

                                 };
        }

        ISagaEntity CreateNewSagaEntity(Type sagaType)
        {
            var sagaEntityType = Configure.GetSagaEntityTypeForSagaType(sagaType);

            if (sagaEntityType == null)
                throw new InvalidOperationException("No saga entity type could be found for saga: " + sagaType);

            var sagaEntity = (ISagaEntity)Activator.CreateInstance(sagaEntityType);

            sagaEntity.Id = GuidCombGenerator.Generate();

            sagaEntity.Originator = Bus.CurrentMessageContext.ReplyToAddress.ToString();
            sagaEntity.OriginalMessageId = Bus.CurrentMessageContext.Id;

            return sagaEntity;
        }

        /// <summary>
        /// Dispatcher factory filters on handler type
        /// </summary>
        /// <param name="handler">handler</param>
        /// <returns>returns true if can be dispatched</returns>
        public bool CanDispatch(Type handler)
        {
            return typeof(ISaga).IsAssignableFrom(handler);
        }

        IEnumerable<IFinder> GetFindersFor(object message, IBuilder builder)
        {
            var sagaId = message.GetHeader(Headers.SagaId);
            var sagaEntityType = GetSagaEntityType(message);

            if (sagaEntityType == null || string.IsNullOrEmpty(sagaId))
            {
                var finders = Configure.GetFindersFor(message).Select(t => builder.Build(t) as IFinder).ToList();

                if (logger.IsDebugEnabled)
                    logger.DebugFormat("The following finders:{0} was allocated to message of type {1}", string.Join(";", finders.Select(t => t.GetType().Name)), message.GetType());

                return finders;
            }

            logger.DebugFormat("Message contains a saga type and saga id. Going to use the saga id finder. Type:{0}, Id:{1}", sagaEntityType, sagaId);

            return new List<IFinder> { builder.Build(typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType)) as IFinder };
        }

        static Type GetSagaEntityType(object message)
        {
            //we keep this for backwards compatibility with versions < 3.0.4
            var sagaEntityType = message.GetHeader(Headers.SagaEntityType);

            if (!string.IsNullOrEmpty(sagaEntityType))
                return Type.GetType(sagaEntityType);

            var sagaTypeName = message.GetHeader(Headers.SagaType);

            if (string.IsNullOrEmpty(sagaTypeName))
                return null;

            var sagaType = Type.GetType(sagaTypeName, false);

            if (sagaType == null)
                return null;

            return Configure.GetSagaEntityTypeForSagaType(sagaType);
        }

        static ISagaEntity UseFinderToFindSaga(IFinder finder, object message)
        {
            MethodInfo method = Configure.GetFindByMethodForFinder(finder, message);

            if (method != null)
                return method.Invoke(finder, new object[] { message }) as ISagaEntity;

            return null;
        }

        void NotifyTimeoutManagerThatSagaHasCompleted(ISaga saga)
        {
            Bus.ClearTimeoutsFor(saga.Entity.Id);
        }

        void LogIfSagaIsFinished(ISaga saga)
        {
            if (saga.Completed)
                logger.Debug(string.Format("{0} {1} has completed.", saga.GetType().FullName, saga.Entity.Id));
        }

        /// <summary>
        /// Get or Set Saga Persister
        /// </summary>
        public ISagaPersister Persister { get; set; }

        /// <summary>
        /// The unicast bus
        /// </summary>
        public IUnicastBus Bus { get; set; }


        readonly ILog logger = LogManager.GetLogger(typeof(SagaDispatcherFactory));

    }
}