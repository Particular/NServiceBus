namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability;

    class RecoverabilityPipelineTerminator : PipelineTerminator<IRecoverabilityContext>
    {
        protected override Task Terminate(IRecoverabilityContext context)
        {
            context.PreventChanges();

            var transportOperations = context.RecoverabilityAction.Execute(
                context.ErrorContext,
                context.Metadata);

            return context.Dispatch(transportOperations.ToList());
            //TODO invoke events here
        }
    }
}
