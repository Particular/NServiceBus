#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

static class PipelineInvoker
{
    public static Func<IBehaviorContext, Task> Build(IBehavior[] behaviors)
    {
        if (behaviors.Length == 0)
        {
            return CompletedRoot;
        }

        InvokerNode? next = null;
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            next = behaviors[i].CreateInvokerNode(next);
        }

        return next!.Invoke;
    }

    static Task CompletedRoot(IBehaviorContext _) => Task.CompletedTask;
}