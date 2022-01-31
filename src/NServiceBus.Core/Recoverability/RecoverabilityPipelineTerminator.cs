namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability;
    using NServiceBus.Transport;

    class RecoverabilityPipelineTerminator : PipelineTerminator<IRecoverabilityContext>
    {
        public RecoverabilityPipelineTerminator(RecoverabilityExecutor recoverabilityExecutor)
        {
            this.recoverabilityExecutor = recoverabilityExecutor;
        }

        protected override Task Terminate(IRecoverabilityContext context)
        {
            context.PreventChanges();

            return recoverabilityExecutor.Invoke(
                context.ErrorContext,
                context.RecoverabilityAction,
                 (transportOperation, token) =>
                 {
                     return context.Dispatch(new List<TransportOperation> { transportOperation });
                 },
                context.CancellationToken);
        }

        readonly RecoverabilityExecutor recoverabilityExecutor;
    }
}
