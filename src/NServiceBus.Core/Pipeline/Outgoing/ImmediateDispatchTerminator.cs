namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ImmediateDispatchTerminator : PipelineTerminator<IDispatchContext>
    {
        public ImmediateDispatchTerminator(IDispatchMessages dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(IDispatchContext context)
        {
            return dispatcher.Dispatch(new TransportOperations(context.Operations.ToArray()), context.Extensions);
        }

        IDispatchMessages dispatcher;
    }
}