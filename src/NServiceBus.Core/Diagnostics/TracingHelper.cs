namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

class TracingHelper
{
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public static async Task TryTraceInvocation<T1>(Activity activity, Func<T1, Task> method, T1 args)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
    {
        if (activity == null)
        {
            await method(args).ConfigureAwait(false);
            return;
        }
        else
        {
            try
            {
                await method(args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: Add an explicit tag for operation canceled
                ActivityDecorator.SetErrorStatus(activity, ex);
                throw;
            }

            activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}