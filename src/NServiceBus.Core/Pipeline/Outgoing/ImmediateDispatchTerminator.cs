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
            var transaction = context.Extensions.GetOrCreate<TransportTransaction>();
            return dispatcher.Dispatch(new TransportOperations(context.Operations.ToArray()), transaction, context.Extensions);
        }

        IDispatchMessages dispatcher;
    }
}