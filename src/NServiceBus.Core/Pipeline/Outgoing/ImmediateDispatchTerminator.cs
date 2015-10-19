namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class ImmediateDispatchTerminator : PipelineTerminator<DispatchContext>
    {
        public ImmediateDispatchTerminator(TransportDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        protected override async Task Terminate(DispatchContext context)
        {
            var opsByTransport = context.Operations.GroupBy(o =>
            {
                string selectedTransport;
                return o.DispatchOptions.AddressTag.TryGet("NServiceBus.Transports.SpecificTransport", out selectedTransport)
                    ? selectedTransport
                    : null;
            }, o => o);

            foreach (var group in opsByTransport)
            {
                if (group.Key == null)
                {
                    await dispatcher.UseDefaultDispatcher(d => d.Dispatch(group, context)).ConfigureAwait(false);
                }
                else
                {
                    await dispatcher.UseDispatcher(group.Key, d => d.Dispatch(group, context)).ConfigureAwait(false);
                }
            }
        }

        TransportDispatcher dispatcher;
    }
}