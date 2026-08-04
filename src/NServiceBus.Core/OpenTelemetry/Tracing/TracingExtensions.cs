#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

static class TracingExtensions
{
    public static Task Invoke<TContext>(this IPipeline<TContext> pipeline, TContext context, Activity? activity, IActivityFactory activityFactory) where TContext : IBehaviorContext
    {
        return activity is null ? pipeline.Invoke(context) : TracePipelineStatus(pipeline, context, activity, activityFactory);

        static async Task TracePipelineStatus(IPipeline<TContext> pipeline, TContext context, Activity activity, IActivityFactory activityFactory)
        {
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            try
            {
                await pipeline.Invoke(context).ConfigureAwait(false);
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activityFactory.RecordError(activity, ex);
                throw;
            }
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
        }
    }
}