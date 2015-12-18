namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Sagas;
    using NServiceBus.Transports;

    class PersistSagaBehavior : Behavior<IInvokeHandlerContext>
    {
        public PersistSagaBehavior(ISagaPersister persister, ICancelDeferredMessages timeoutCancellation)
        {
            sagaPersister = persister;
            this.timeoutCancellation = timeoutCancellation;
        }

        public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            ActiveSagaInstance sagaInstanceState;
            if (!context.Extensions.TryGet(out sagaInstanceState))
            {
                return;
            }

            if (sagaInstanceState.NotFound)
            {
                logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.MessageMetadata.MessageType.FullName);

                foreach (var handler in context.Builder.BuildAll<IHandleSagaNotFound>())
                {
                    logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                    await handler.Handle(context.MessageBeingHandled, context).ConfigureAwait(false);
                }
                return;
            }

            var saga = sagaInstanceState.Instance;
            if (saga.Completed)
            {
                if (!sagaInstanceState.IsNew)
                {
                    await sagaPersister.Complete(saga.Entity, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                }

                if (saga.Entity.Id != Guid.Empty)
                {
                    await timeoutCancellation.CancelDeferredMessages(saga.Entity.Id.ToString(), context).ConfigureAwait(false);
                }

                logger.DebugFormat("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.Metadata.Name, saga.Entity.Id);
            }
            else
            {
                sagaInstanceState.ValidateChanges();

                if (sagaInstanceState.IsNew)
                {
                    ActiveSagaInstance.CorrelationPropertyInfo correlationProperty;
                    var sagaCorrelationProperty = SagaCorrelationProperty.None;

                    if (sagaInstanceState.TryGetCorrelationProperty(out correlationProperty))
                    {
                        sagaCorrelationProperty = new SagaCorrelationProperty(correlationProperty.PropertyInfo.Name,correlationProperty.Value);
                    }

                    await sagaPersister.Save(saga.Entity, sagaCorrelationProperty, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                }
                else
                {
                    await sagaPersister.Update(saga.Entity, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                }
            }
        }

        ISagaPersister sagaPersister;
        ICancelDeferredMessages timeoutCancellation;

        static ILog logger = LogManager.GetLogger<PersistSagaBehavior>();

        public class Registration : RegisterStep
        {
            public Registration() : base(WellKnownStep.InvokeSaga, typeof(PersistSagaBehavior), "Persists the previously loaded saga.")
            {
            }
        }
    }
}