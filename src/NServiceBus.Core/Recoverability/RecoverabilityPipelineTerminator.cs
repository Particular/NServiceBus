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

        protected override async Task Terminate(IRecoverabilityContext context)
        {
            //TODO: figure out a better way
            ((RecoverabilityContext)context).ActionToTake = await recoverabilityExecutor.Invoke(context).ConfigureAwait(false);
        }

        readonly RecoverabilityExecutor recoverabilityExecutor;
    }
}
