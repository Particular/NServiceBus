#nullable enable

namespace NServiceBus;

using System.Linq;
using System.Threading.Tasks;
using Pipeline;
using Transport;

class ImmediateDispatchTerminator(IMessageDispatcher dispatcher) : PipelineTerminator<IDispatchContext>
{
    protected override async Task Terminate(IDispatchContext context)
    {
        var transaction = context.Extensions.GetOrCreate<TransportTransaction>();
        var operations = context.Operations as TransportOperation[] ?? context.Operations.ToArray();

        // Deliberately no try/finally: after a failed dispatch, ownership of the headers is ambiguous
        // (partial transport consumption, observers), so they leak to the GC instead of being pooled.
        await dispatcher.Dispatch(new TransportOperations(operations), transaction, context.CancellationToken).ConfigureAwait(false);

        // Behaviors can still read the headers while unwinding, so the owning pipeline invocation returns them when it completes.
        var owner = PipelineRootContext.FindOwner(context.Extensions);
        if (owner is null)
        {
            return;
        }

        foreach (var operation in operations)
        {
            owner.DeferHeaderReturn(operation.Message.Headers);
        }
    }
}