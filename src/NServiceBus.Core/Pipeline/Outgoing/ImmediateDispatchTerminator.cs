namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class ImmediateDispatchTerminator : PipelineTerminator<DispatchContext>
    {
        public ImmediateDispatchTerminator(TransportDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(DispatchContext context)
        {
            return dispatcher.UseDispatcher(d => d.Dispatch(context.Operations, context));
        }

        TransportDispatcher dispatcher;
    }
}