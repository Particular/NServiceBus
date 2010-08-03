using System;
using NServiceBus.ObjectBuilder;
using System.Reflection;
using Common.Logging;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
	/// <summary>
    /// A message handler central to the saga infrastructure.
	/// </summary>
    public class SagaMessageHandler : IMessageHandler<IMessage>
    {
        /// <summary>
        /// Used to notify timeout manager of sagas that have completed.
        /// </summary>
        public IBus Bus { get; set; }

		/// <summary>
		/// Handles a message.
		/// </summary>
		/// <param name="message">The message to handle.</param>
		/// <remarks>
		/// If the message received needs to start a new saga, then a new
		/// saga instance will be created and will be saved using the <see cref="ISagaPersister"/>
		/// implementation provided in the configuration.  Any other message implementing 
		/// <see cref="ISagaMessage"/> will cause the existing saga instance with which it is
		/// associated to continue.</remarks>
        public void Handle(IMessage message)
        {
            if (!NeedToHandle(message))
                return;

            var entitiesHandled = new List<ISagaEntity>();
		    var sagaTypesHandled = new List<Type>();

            foreach (IFinder finder in Configure.GetFindersFor(message))
            {
                ISaga saga;
                bool sagaEntityIsPersistent = true;
                ISagaEntity sagaEntity = UseFinderToFindSaga(finder, message);

                if (sagaEntity == null)
                {
                    Type sagaToCreate = Configure.GetSagaTypeToStartIfMessageNotFoundByFinder(message, finder);
                    if (sagaToCreate == null)
                        continue;

                    if (sagaTypesHandled.Contains(sagaToCreate))
                        continue; // don't create the same saga type twice for the same message

                    sagaTypesHandled.Add(sagaToCreate);

                    Type sagaEntityType = Configure.GetSagaEntityTypeForSagaType(sagaToCreate);
                    sagaEntity = Activator.CreateInstance(sagaEntityType) as ISagaEntity;

                    if (sagaEntity != null)
                    {
                        if (message is ISagaMessage)
                            sagaEntity.Id = (message as ISagaMessage).SagaId;
                        else
                            sagaEntity.Id = GenerateSagaId();

                        sagaEntity.Originator = Bus.CurrentMessageContext.ReturnAddress;
                        sagaEntity.OriginalMessageId = Bus.CurrentMessageContext.Id;

                        sagaEntityIsPersistent = false;
                    }

                    saga = Builder.Build(sagaToCreate) as ISaga;
                    
                }
                else
                {
                    if (entitiesHandled.Contains(sagaEntity))
                        continue; // don't call the same saga twice

                    saga = Builder.Build(Configure.GetSagaTypeForSagaEntityType(sagaEntity.GetType())) as ISaga;
                }

                if (saga != null)
                {
                    saga.Entity = sagaEntity;

                    HaveSagaHandleMessage(saga, message, sagaEntityIsPersistent);

                    sagaTypesHandled.Add(saga.GetType());
                }

                entitiesHandled.Add(sagaEntity);
            }

            if (entitiesHandled.Count == 0)
            {
                foreach(var handler in NServiceBus.Configure.Instance.Builder.BuildAll<IHandleSagaNotFound>())
                    handler.Handle(message);
            }
        }

        /// <summary>
        /// Decides whether the given message should be handled by the saga infrastructure
        /// </summary>
        /// <param name="message">The message being processed</param>
        /// <returns></returns>
	    public bool NeedToHandle(IMessage message)
	    {
            if (message is ISagaMessage && !(message is TimeoutMessage))
                return true;

            var tm = message as TimeoutMessage;
            if (tm != null)
            {
                if (tm.HasNotExpired())
                {
                    Bus.HandleCurrentMessageLater();
                    return false;
                }

                return true;
            }

            if (Configure.IsMessageTypeHandledBySaga(message.GetType()))
                return true;

            return false;
	    }

        /// <summary>
        /// Generates a new id for a saga.
        /// </summary>
        /// <returns></returns>
        public virtual Guid GenerateSagaId()
        {
            return GuidCombGenerator.Generate();
        }

        #region helper methods

        /// <summary>
        /// Asks the given finder to find the saga entity using the given message.
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="message"></param>
        /// <returns>The saga entity if found, otherwise null.</returns>
        private static ISagaEntity UseFinderToFindSaga(IFinder finder, IMessage message)
        {
            MethodInfo method = Configure.GetFindByMethodForFinder(finder, message);

            if (method != null)
                return method.Invoke(finder, new object[] {message}) as ISagaEntity;

            return null;
        }

        /// <summary>
        /// Dispatches the message to the saga and, based on the saga's state
        /// persists it or notifies of its completion to interested parties.
        /// </summary>
        /// <param name="saga"></param>
        /// <param name="message"></param>
        /// <param name="sagaIsPersistent"></param>
        protected virtual void HaveSagaHandleMessage(ISaga saga, IMessage message, bool sagaIsPersistent)
        {
            var tm = message as TimeoutMessage;
            if (tm != null)
                saga.Timeout(tm.State);
            else
                CallHandleMethodOnSaga(saga, message);

            if (!saga.Completed)
            {
                if (!sagaIsPersistent)
                    Persister.Save(saga.Entity);
                else
                    Persister.Update(saga.Entity);
            }
            else
            {
                if (sagaIsPersistent)
                    Persister.Complete(saga.Entity);

                NotifyTimeoutManagerThatSagaHasCompleted(saga);
            }

            LogIfSagaIsFinished(saga);
        }

        /// <summary>
        /// Notifies the timeout manager of the saga's completion by sending a timeout message.
        /// </summary>
        /// <param name="saga"></param>
	    protected virtual void NotifyTimeoutManagerThatSagaHasCompleted(ISaga saga)
	    {
	        Bus.Send(new TimeoutMessage(saga.Entity, true));
	    }

	    /// <summary>
		/// Logs that a saga has completed.
		/// </summary>
        protected virtual void LogIfSagaIsFinished(ISaga saga)
        {
            if (saga.Completed)
                logger.Debug(string.Format("{0} {1} has completed.", saga.GetType().FullName, saga.Entity.Id));
        }

		/// <summary>
		/// Invokes the handler method on the saga for the message.
		/// </summary>
		/// <param name="saga">The saga on which to call the handle method.</param>
		/// <param name="message">The message to pass to the handle method.</param>
        protected virtual void CallHandleMethodOnSaga(object saga, IMessage message)
        {
		    MethodInfo method = Configure.GetHandleMethodForSagaAndMessage(saga, message);

            if (method != null)
                method.Invoke(saga, new object[] { message });
        }

        #endregion

        #region config info

		/// <summary>
		/// Gets/sets the builder that will be used for instantiating sagas.
		/// </summary>
        public virtual IBuilder Builder { get; set; }

        /// <summary>
        /// Gets/sets the object used to persist and retrieve sagas.
        /// </summary>
        public virtual ISagaPersister Persister { get; set; }

        #endregion

        /// <summary>
        /// Object used to log information.
        /// </summary>
        protected readonly ILog logger = LogManager.GetLogger(typeof(SagaMessageHandler));
    }
}
