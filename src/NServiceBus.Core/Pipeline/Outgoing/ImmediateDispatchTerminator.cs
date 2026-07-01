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
        try
        {
            await dispatcher.Dispatch(new TransportOperations(operations), transaction, context.CancellationToken).ConfigureAwait(false);
        }
        finally
        {
            foreach (var operation in operations)
            {
                DictionaryPool<string, string>.Shared.Return(operation.Message.Headers);
            }
        }
    }
}