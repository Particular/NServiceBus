#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Logging;
using Pipeline;

class Pipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
{
    public Pipeline(IServiceProvider builder, PipelineModifications pipelineModifications)
    {
        var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Additions, pipelineModifications.Replacements, pipelineModifications.AdditionsOrReplacements);

        // Important to keep a reference
        behaviors = [.. coordinator.BuildPipelineModelFor<TContext>()
            .Select(r => r.CreateBehavior(builder))];

        List<Expression>? expressions = null;
        if (Logger.IsDebugEnabled)
        {
            expressions = [];
        }

        pipeline = behaviors.CreatePipelineExecutionFuncFor<TContext>(expressions);

        if (Logger.IsDebugEnabled && expressions is not null)
        {
            Logger.Debug(expressions.PrettyPrint());
        }
    }

    public Task Invoke(TContext context)
    {
        // The pipeline sets the behaviors to the context bag for the current stage so that the next delegates
        // can extract the pipeline behaviors. This avoids costly closure allocations. This is safe because
        // the behavior order is fixed once the pipeline is baked.
        context.Extensions.Behaviors = behaviors;
        return pipeline(context);
    }

    readonly IBehavior[] behaviors;
    readonly Func<TContext, Task> pipeline;

    static readonly ILog Logger = LogManager.GetLogger<Pipeline<TContext>>();
}