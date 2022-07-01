namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

static class TracingExtensions
{
    public static Task Invoke<TContext>(this IPipeline<TContext> pipeline, TContext context, Activity activity) where TContext : IBehaviorContext
    {
        return activity == null ? pipeline.Invoke(context) : TracePipelineStatus();

        async Task TracePipelineStatus()
        {
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            try
            {
                await pipeline.Invoke(context).ConfigureAwait(false);
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                // TODO: Add an explicit tag for operation canceled
                activity.SetErrorStatus(ex);
                throw;
            }
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
        }
    }
}