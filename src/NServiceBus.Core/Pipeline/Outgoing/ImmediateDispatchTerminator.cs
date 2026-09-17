#nullable enable

namespace NServiceBus;

using System.Linq;
using System.Threading.Tasks;
using Pipeline;
using Transport;

class ImmediateDispatchTerminator(IMessageDispatcher dispatcher, HeaderPool headerPool) : PipelineTerminator<IDispatchContext>
{
    protected override async Task Terminate(IDispatchContext context)
    {
        var transaction = context.Extensions.GetOrCreate<TransportTransaction>();
        var operations = context.Operations as TransportOperation[] ?? context.Operations.ToArray();

        // Deliberately no try/finally: after a failed dispatch, ownership of the headers is ambiguous
        // (partial transport consumption, observers), so they leak to the GC instead of being pooled.
        await dispatcher.Dispatch(new TransportOperations(operations), transaction, context.CancellationToken).ConfigureAwait(false);

        foreach (var operation in operations)
        {
            headerPool.Return(operation.Message.Headers);
        }
    }
}