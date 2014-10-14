namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Finders;
    using IdGeneration;
    using Logging;
    using ObjectBuilder;
    using Saga;
    using Transports;
    using Unicast;

    /// <summary>
    /// Dispatch factory that can dispatch messages to sagas
    /// </summary>
    public class SagaDispatcherFactory : IMessageDispatcherFactory
    {

        bool IsAllowedToStartANewSaga(Type sagaInstanceType)
        {
            string sagaType;

            if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.SagaId) &&
                Bus.CurrentMessageContext.Headers.TryGetValue(Headers.SagaType, out sagaType))
            {
                //we want to move away from the assembly fully qualified name since that will break if you move sagas
                // between assemblies. We use the fullname instead which is enough to identify the saga
                if (sagaType.StartsWith(sagaInstanceType.FullName))
                {
                    //so now we have a saga id for this saga and if we can't find it we shouldn't start a new one
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get Dispatcher
        /// </summary>
        /// <param name="messageHandlerType">Type of the message Handler</param>
        /// <param name="builder">Builder</param>
        /// <param name="message">Message</param>
        /// <returns>Saga Dispatcher</returns>
        public IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object message)
        {
            var entitiesHandled = new List<IContainSagaData>();
            var sagaTypesHandled = new List<Type>();

            foreach (var finder in GetFindersFor(message, builder))
            {
                var sagaEntityIsPersistent = true;
                var sagaEntity = UseFinderToFindSaga(finder, message);
                Type sagaType;

                if (sagaEntity == null)
                {
                    sagaType = Features.Sagas.GetSagaTypeToStartIfMessageNotFoundByFinder(message, finder);
                    if (sagaType == null)
                        continue;

                    if (sagaTypesHandled.Contains(sagaType))
                        continue; // don't create the same saga type twice for the same message

                    if (!IsAllowedToStartANewSaga(sagaType))
                        continue;

                    sagaEntity = CreateNewSagaEntity(sagaType);

                    sagaEntityIsPersistent = false;
                }
                else
                {
                    if (entitiesHandled.Contains(sagaEntity))
                        continue; // don't call the same saga twice

                    sagaType = Features.Sagas.GetSagaTypeForSagaEntityType(sagaEntity.GetType());
                }

                if (messageHandlerType.IsAssignableFrom(sagaType))
                    yield return () =>
                                     {
                                         var saga = (ISaga)builder.Build(sagaType);

                                         saga.Entity = sagaEntity;

                                         try
                                         {
                                             SagaContext.Current = saga;

                                             if (IsTimeoutMessage(message))
                                             {
												 HandlerInvocationCache.InvokeTimeout(saga, message);
                                             }
                                             else
                                             {
												 HandlerInvocationCache.InvokeHandle(saga, message);
                                             }

                                             if (!saga.Completed)
                                             {
                                                 if (!sagaEntityIsPersistent)
                                                 {
                                                     Persister.Save(saga.Entity);
                                                 }
                                                 else
                                                 {
                                                     Persister.Update(saga.Entity);
                                                 }
                                             }
                                             else
                                             {
                                                 if (sagaEntityIsPersistent)
                                                 {
                                                     Persister.Complete(saga.Entity);
                                                 }

                                                 if (saga.Entity.Id != Guid.Empty)
                                                 {
                                                     NotifyTimeoutManagerThatSagaHasCompleted(saga);
                                                 }
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

        IContainSagaData CreateNewSagaEntity(Type sagaType)
        {
            var sagaEntityType = Features.Sagas.GetSagaEntityTypeForSagaType(sagaType);

            if (sagaEntityType == null)
                throw new InvalidOperationException("No saga entity type could be found for saga: " + sagaType);

            var sagaEntity = (IContainSagaData)Activator.CreateInstance(sagaEntityType);

            sagaEntity.Id = CombGuid.Generate();

            if (Bus.CurrentMessageContext.ReplyToAddress != null)
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
            var sagaId = Headers.GetMessageHeader(message, Headers.SagaId);
            var sagaEntityType = GetSagaEntityType(message);

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

        static Type GetSagaEntityType(object message)
        {
#pragma warning disable 0618
            //we keep this for backwards compatibility with versions < 3.0.4
            var sagaEntityType = Headers.GetMessageHeader(message, Headers.SagaEntityType);
#pragma warning restore 0618

            if (!string.IsNullOrEmpty(sagaEntityType))
                return Type.GetType(sagaEntityType);

            var sagaTypeName = Headers.GetMessageHeader(message, Headers.SagaType);

            if (string.IsNullOrEmpty(sagaTypeName))
                return null;

            var sagaType = Type.GetType(sagaTypeName, false);

            if (sagaType == null)
                return null;

            return Features.Sagas.GetSagaEntityTypeForSagaType(sagaType);
        }

        static IContainSagaData UseFinderToFindSaga(IFinder finder, object message)
        {
            var method = Features.Sagas.GetFindByMethodForFinder(finder, message);

            if (method != null)
                return method.Invoke(finder, new [] { message }) as IContainSagaData;

            return null;
        }

        void NotifyTimeoutManagerThatSagaHasCompleted(ISaga saga)
        {
            MessageDeferrer.ClearDeferredMessages(Headers.SagaId, saga.Entity.Id.ToString());
        }

        void LogIfSagaIsFinished(ISaga saga)
        {
            if (saga.Completed)
                logger.Debug(string.Format("{0} {1} has completed.", saga.GetType().FullName, saga.Entity.Id));
        }

        /// <summary>
        /// True if this is a timeout message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        static bool IsTimeoutMessage(object message)
        {
            return !string.IsNullOrEmpty(Headers.GetMessageHeader(message, Headers.IsSagaTimeoutMessage));
        }

        /// <summary>
        /// Get or Set Saga Persister
        /// </summary>
        public ISagaPersister Persister { get; set; }

        /// <summary>
        /// The unicast bus
        /// </summary>
        public IUnicastBus Bus { get; set; }

        /// <summary>
        /// A way to request the transport to defer the processing of a message
        /// </summary>
        public IDeferMessages MessageDeferrer { get; set; }

        readonly ILog logger = LogManager.GetLogger(typeof(SagaDispatcherFactory));

    }
}
