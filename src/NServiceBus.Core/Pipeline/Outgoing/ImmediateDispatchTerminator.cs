namespace NServiceBus
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ImmediateDispatchTerminator : PipelineTerminator<IDispatchContext>
    {
        public ImmediateDispatchTerminator(IDispatchMessages dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(IDispatchContext context, CancellationToken cancellationToken)
        {
            var transaction = context.Extensions.GetOrCreate<TransportTransaction>();
            var operations = context.Operations as TransportOperation[] ?? context.Operations.ToArray();
            return dispatcher.Dispatch(new TransportOperations(operations), transaction, context.Extensions);
        }

        readonly IDispatchMessages dispatcher;
    }
}