#nullable enable

namespace NServiceBus;

using System.Threading.Tasks;
using Pipeline;
using Transport;

class ImmediateDispatchTerminator(IMessageDispatcher dispatcher) : PipelineTerminator<IDispatchContext>
{
    protected override Task Terminate(IDispatchContext context)
    {
        var transaction = context.Extensions.GetOrCreate<TransportTransaction>();
        var operations = context.Operations as TransportOperation[] ?? [.. context.Operations];
        return dispatcher.Dispatch(new TransportOperations(operations), transaction, context.CancellationToken);
    }
}