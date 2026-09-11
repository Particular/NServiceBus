#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

/// <summary>
/// Wraps a pipeline in an activity that spans the whole invocation. The activity is started unconditionally on every
/// invocation: whether one is actually created is decided by the <see cref="ActivitySource"/> listeners at call time,
/// which can change while the endpoint is running, so no attempt is made to bypass the wrapper when tracing is off.
/// The cost of the wrapper without a listener is a single async frame.
/// </summary>
sealed class TracedPipeline<TContext>(
    IPipeline<TContext> inner,
    IActivityFactory activityFactory,
    Func<IActivityFactory, TContext, Activity?> startActivity) : IPipeline<TContext>
    where TContext : IBehaviorContext
{
    public IPipeline<TContext> Inner { get; } = inner;

    public async Task Invoke(TContext context)
    {
        using var activity = startActivity(activityFactory, context);

#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - recording and rethrowing
        try
        {
            await Inner.Invoke(context).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activityFactory.RecordError(activity, ex, context.Extensions);
            throw;
        }
#pragma warning restore PS0019
    }
}
