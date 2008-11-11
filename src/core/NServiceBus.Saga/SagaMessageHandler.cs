using System;
using ObjectBuilder;
using System.Reflection;
using Common.Logging;

namespace NServiceBus.Saga
{
	/// <summary>
    /// A message handler that supports sagas.
	/// </summary>
    public class SagaMessageHandler : BaseMessageHandler<IMessage>
    {
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
        public override void Handle(IMessage message)
        {
            if (!this.NeedToHandle(message))
                return;

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

                    Type sagaEntityType = Configure.GetSagaEntityTypeForSagaType(sagaToCreate);
                    sagaEntity = Activator.CreateInstance(sagaEntityType) as ISagaEntity;
                    sagaEntity.Id = this.GenerateSagaId();
                    sagaEntity.Originator = Bus.SourceOfMessageBeingHandled;

                    sagaEntityIsPersistent = false;

                    saga = builder.Build(sagaToCreate) as ISaga;
                }
                else
                    saga = builder.Build(Configure.GetSagaTypeForSagaEntityType(sagaEntity.GetType())) as ISaga;

                saga.Entity = sagaEntity;

                HaveSagaHandleMessage(saga, message, sagaEntityIsPersistent);
            }
        }

	    public bool NeedToHandle(IMessage message)
	    {
            if (message is ISagaMessage && !(message is TimeoutMessage))
                return true;

            TimeoutMessage tm = message as TimeoutMessage;
            if (tm != null)
            {
                if (tm.HasNotExpired())
                {
                    this.Bus.HandleCurrentMessageLater();
                    return false;
                }
                else
                    return true;
            }

            if (Configure.IsMessageTypeHandledBySaga(message.GetType()))
                return true;

            return false;
	    }

        public virtual Guid GenerateSagaId()
        {
            return GuidCombGenerator.Generate();
        }

        #region helper methods

        private ISagaEntity UseFinderToFindSaga(IFinder finder, IMessage message)
        {
            MethodInfo method = Configure.GetFindByMethodForFinder(finder, message);

            if (method != null)
                return method.Invoke(finder, new object[] {message}) as ISagaEntity;

            return null;
        }

        protected virtual void HaveSagaHandleMessage(ISaga saga, IMessage message, bool sagaIsPersistent)
        {
            TimeoutMessage tm = message as TimeoutMessage;
            if (tm != null)
                saga.Timeout(tm.State);
            else
                CallHandleMethodOnSaga(saga, message);

            if (!saga.Completed)
            {
                using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
                    if (!sagaIsPersistent)
                        persister.Save(saga.Entity);
                    else
                        persister.Update(saga.Entity);
            }
            else
            {
                if (sagaIsPersistent)
                    using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
                        persister.Complete(saga.Entity);

                NotifyTimeoutManagerThatSagaHasCompleted(saga);
            }

            LogIfSagaIsFinished(saga);
        }

	    protected virtual void NotifyTimeoutManagerThatSagaHasCompleted(ISaga saga)
	    {
	        this.Bus.Send(new TimeoutMessage(saga.Entity, true));
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

        private IBuilder builder;

		/// <summary>
		/// Gets/sets an <see cref="IBuilder"/> that will be used for resolving
		/// the <see cref="ISagaPersister"/> implementation to be used for
		/// saga persistence.
		/// </summary>
        public virtual IBuilder Builder
        {
            get { return builder; }
            set { builder = value; }
        }

        #endregion

        protected readonly ILog logger = LogManager.GetLogger(typeof(SagaMessageHandler));
    }
}
