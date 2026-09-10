#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

static class TracingExtensions
{
    public static Task Invoke<TContext>(this IPipeline<TContext> pipeline, TContext context, Activity? activity, IActivityFactory activityFactory) where TContext : IBehaviorContext
    {
        //TODO: metric tags collection should always be there no matter activity
        return activity is null ? pipeline.Invoke(context) : TracePipelineStatus(pipeline, context, activity, activityFactory);

        static async Task TracePipelineStatus(IPipeline<TContext> pipeline, TContext context, Activity activity, IActivityFactory activityFactory)
        {
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            try
            {
                var incomingPipelineMetrics = context.Builder.GetRequiredService<IncomingPipelineMetrics>();
                var metricsTags = ((IBehaviorContext)context.Extensions).OutgoingMetricTags;

                incomingPipelineMetrics.AddDefaultIncomingPipelineMetricTags(metricsTags);

                await pipeline.Invoke(context).ConfigureAwait(false);
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activityFactory.RecordError(activity, ex, context.Extensions);
                throw;
            }
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
        }
    }
}