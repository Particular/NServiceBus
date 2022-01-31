namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class RecoverabilityPipelineTerminator : PipelineTerminator<IRecoverabilityContext>
    {
        public RecoverabilityPipelineTerminator(RecoverabilityExecutor recoverabilityExecutor)
        {
            this.recoverabilityExecutor = recoverabilityExecutor;
        }

        protected override Task Terminate(IRecoverabilityContext context)
        {
            context.PreventChanges();

            return recoverabilityExecutor.Invoke(context);
        }

        readonly RecoverabilityExecutor recoverabilityExecutor;
    }
}
