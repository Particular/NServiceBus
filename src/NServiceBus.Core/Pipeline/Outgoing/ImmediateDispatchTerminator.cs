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

        // Headers of a message dispatched from within its own send, publish or reply pipeline are still
        // visible to that pipeline's behaviors, so the pipeline returns them once it has completed.
        _ = context.Extensions.TryGet(out OutgoingRootContext? owner);

        foreach (var operation in operations)
        {
            if (!ReferenceEquals(operation.Message.Headers, owner?.Headers))
            {
                headerPool.Return(operation.Message.Headers);
            }
        }
    }
}