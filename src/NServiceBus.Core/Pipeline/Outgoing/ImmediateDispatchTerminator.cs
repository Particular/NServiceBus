﻿namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ImmediateDispatchTerminator : PipelineTerminator<IDispatchContext>
    {
        public ImmediateDispatchTerminator(IMessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(IDispatchContext context)
        {
            var transaction = context.Extensions.GetOrCreate<TransportTransaction>();
            var operations = context.Operations as TransportOperation[] ?? context.Operations.ToArray();
            return dispatcher.Dispatch(new TransportOperations(operations), transaction, context.CancellationToken);
        }

        readonly IMessageDispatcher dispatcher;
    }
}