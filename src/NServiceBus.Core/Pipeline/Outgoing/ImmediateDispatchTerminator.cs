namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class ImmediateDispatchTerminator : PipelineTerminator<ImmediateDispatchContext>
    {
        public ImmediateDispatchTerminator(IDispatchMessages dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(ImmediateDispatchContext context)
        {
            return dispatcher.Dispatch(context.Operations, context);
        }

        IDispatchMessages dispatcher;
    }
}