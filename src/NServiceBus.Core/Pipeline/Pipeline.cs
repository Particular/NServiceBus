#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using Pipeline;

class Pipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
{
    public Pipeline(IServiceProvider builder, PipelineModifications pipelineModifications)
    {
        var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Additions, pipelineModifications.Replacements, pipelineModifications.AdditionsOrReplacements);

        var pipelineBuildModel = coordinator.BuildPipelineBuildModelFor<TContext>();
        var registrations = pipelineBuildModel.Steps;

        // Important to keep a reference
        behaviors = [.. registrations.Select(r => r.CreateBehavior(builder))];

        parts = PipelinePartBuilder.BuildParts(pipelineBuildModel);

        if (Logger.IsDebugEnabled)
        {
            Logger.Debug(PipelinePartDiagnostics.PrettyPrint(parts));
        }
    }

    public Task Invoke(TContext context)
    {
        // The pipeline sets the behaviors and the parts to the context bag for the current stage so that the next delegates
        // can extract the pipeline behaviors. This avoids costly closure allocations. This is safe because
        // the behavior order is fixed once the pipeline is baked.
        context.Extensions.Initialize(behaviors, parts);
        return PipelineRunner.Start(context);
    }

    readonly IBehavior[] behaviors;
    readonly PipelinePart[] parts;

    static readonly ILog Logger = LogManager.GetLogger<Pipeline<TContext>>();
}