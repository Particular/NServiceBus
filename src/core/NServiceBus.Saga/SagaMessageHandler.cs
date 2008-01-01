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
            if (message is ISagaMessage)
            {
                this.HandleContinuing(message as ISagaMessage);
                return;
            }

            if (message.GetType().GetCustomAttributes(typeof(StartsSagaAttribute), true).Length > 0)
                this.HandleStartingWorkflow(message);
        }

		/// <summary>
		/// Creates and persists the appropriate saga entity for the provided message.
		/// </summary>
		/// <param name="message">The message to start a saga for.</param>
        private void HandleStartingWorkflow(IMessage message)
        {
            Type specificWorkflowType = GetSagaTypeForMessage(message);

            ISagaEntity saga = this.builder.Build(specificWorkflowType) as ISagaEntity;

            if (saga == null)
            {
                logger.Debug("Could not find a saga type that corresponds to " + specificWorkflowType.FullName);
                return;
            }

            saga.Id = GuidCombGenerator.Generate();

		    using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
            {
                HandleMessageOnSaga(saga, message);

                if (saga.Completed)
                    persister.Complete(saga);
                else
                    persister.Save(saga);
            }

            LogIfSagaCompleted(saga);
        }

		/// <summary>
		/// Gets the workflow entity associated with the message from the persistence
		/// store and handles it.
		/// </summary>
		/// <param name="message">The message to handle.</param>
        private void HandleContinuing(ISagaMessage message)
        {
            TimeoutMessage tm = message as TimeoutMessage;

            ISagaEntity saga;
            using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
            {
                saga = persister.Get(message.SagaId);
                
                if (saga == null)
                    return;

                if (tm != null && tm.HasNotExpired())
                {
                    this.Bus.HandleCurrentMessageLater();
                    return;
                }

                if (tm != null)
                    saga.Timeout(tm.State);
                else
                    HandleMessageOnSaga(saga, message);

                if (saga.Completed)
                    persister.Complete(saga);
                else
                    persister.Update(saga);
            }

            LogIfSagaCompleted(saga);
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
        private static void HandleMessageOnSaga(object saga, IMessage message)
        {
            MethodInfo method = saga.GetType().GetMethod("Handle", new Type[] { message.GetType() });

            if (method != null)
                method.Invoke(saga, new object[] { message });
        }

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
