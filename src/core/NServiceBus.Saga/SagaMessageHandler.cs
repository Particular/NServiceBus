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
		/// If the message received is has the <see cref="StartsSagaAttribute"/> defined then a new
		/// workflow instance will be created and will be saved using the <see cref="ISagaPersister"/>
		/// implementation provided in the configuration.  Any other message implementing 
		/// <see cref="ISagaMessage"/> will cause the existing saga instance with which it is
		/// associated to continue.</remarks>
        public override void Handle(IMessage message)
        {
            if (!this.NeedToHandle(message))
                return;

		    bool sagaIsPersistent = true;

		    ISagaEntity saga = this.FindSagaUsing(message);
            if (saga == null && message is ISagaMessage)
                return; //couldn't find saga for ISagaMessage - do nothing

            if (saga == null)
            {
                Type sagaType = GetSagaTypeForMessage(message);
                saga = this.builder.Build(sagaType) as ISagaEntity;

                // intentionally don't check for null - need to do handle this message
                // if a saga couldn't be found or created, the exception will cause
                // the message to return to the queue.
                // if this happens every time, the message will be moved to the
                // error queue, and the admin will know that this endpoint isn't
                // configured properly.
                saga.Id = this.GenerateSagaId();

                sagaIsPersistent = false;
            }

            HaveSagaHandleMessage(saga, message, sagaIsPersistent);
        }

	    public virtual bool NeedToHandle(IMessage message)
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

	        if (message.GetType().GetCustomAttributes(typeof(StartsSagaAttribute), true).Length > 0)
                return true;

	        return false;
	    }

	    public virtual ISagaEntity FindSagaUsing(IMessage message)
	    {
	        ISagaMessage sagaMessage = message as ISagaMessage;

            if (sagaMessage != null)
                using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
                    return persister.Get(sagaMessage.SagaId);
	        
            return null;
	    }

        public virtual Guid GenerateSagaId()
        {
            return GuidCombGenerator.Generate();
        }

        #region helper methods

        private void HaveSagaHandleMessage(ISagaEntity saga, IMessage message, bool sagaIsPersistent)
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
                        persister.Save(saga);
                    else
                        persister.Update(saga);
            }
            else
            {
                if (sagaIsPersistent)
                    using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
                        persister.Complete(saga);

                NotifyTimeoutManagerThatSagaHasCompleted(saga);
            }

            LogIfSagaCompleted(saga);

        }

	    private void NotifyTimeoutManagerThatSagaHasCompleted(ISagaEntity saga)
	    {
	        this.Bus.Send(new TimeoutMessage(saga, true));
	    }

	    /// <summary>
		/// Logs that a saga has completed.
		/// </summary>
		/// <param name="saga">The saga that completed.</param>
        private static void LogIfSagaCompleted(ISagaEntity saga)
        {
            if (saga.Completed)
                logger.Debug(string.Format("{0} {1} has completed.", saga.GetType().FullName, saga.Id));
        }

		/// <summary>
		/// Gets the type of saga associated with the specified message.
		/// </summary>
		/// <param name="message">The message to get the saga type for.</param>
        /// <returns>The saga type associated with the message.</returns>
        private static Type GetSagaTypeForMessage(IMessage message)
        {
            return typeof(ISaga<>).MakeGenericType(message.GetType());
        }

		/// <summary>
		/// Invokes the handler method on the saga for the message.
		/// </summary>
		/// <param name="saga">The saga on which to call the handle method.</param>
		/// <param name="message">The message to pass to the handle method.</param>
        private static void CallHandleMethodOnSaga(ISagaEntity saga, IMessage message)
        {
            MethodInfo method = saga.GetType().GetMethod("Handle", new Type[] { message.GetType() });

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
        public IBuilder Builder
        {
            get { return builder; }
            set { builder = value; }
        }

        #endregion

        private static readonly ILog logger = LogManager.GetLogger(typeof(SagaMessageHandler));
    }
}
