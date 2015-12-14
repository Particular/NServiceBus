namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class ImmediateDispatchTerminator : PipelineTerminator<IDispatchContext>
    {
        public ImmediateDispatchTerminator(IDispatchMessages dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(IDispatchContext context)
        {
            return dispatcher.Dispatch(context.Operations, context.Extensions);
        }

        IDispatchMessages dispatcher;
    }
}