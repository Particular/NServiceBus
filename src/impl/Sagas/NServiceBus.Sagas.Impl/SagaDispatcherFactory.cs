namespace NServiceBus.Sagas.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Common.Logging;
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
            if (message.IsTimeoutMessage() && !message.TimeoutHasExpired())
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


                    Type sagaEntityType = Configure.GetSagaEntityTypeForSagaType(sagaType);
                    sagaEntity = Activator.CreateInstance(sagaEntityType) as ISagaEntity;

                    if (sagaEntity != null)
                    {
                        if (message is ISagaMessage)
                            sagaEntity.Id = (message as ISagaMessage).SagaId;
                        else
                            sagaEntity.Id = GuidCombGenerator.Generate();

                        sagaEntity.Originator = Bus.CurrentMessageContext.ReplyToAddress.ToString();
                        sagaEntity.OriginalMessageId = Bus.CurrentMessageContext.Id;

                        sagaEntityIsPersistent = false;
                    }
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

                                         HandlerInvocationCache.Invoke(saga, message);

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
            return Configure.GetFindersFor(message).Select(t => builder.Build(t) as IFinder);
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