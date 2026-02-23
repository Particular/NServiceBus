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

        var registrations = coordinator.BuildPipelineFor<TContext>();

        IBehavior[] behaviors = [.. registrations.Select(r => r.CreateBehavior(builder))];

        if (Logger.IsDebugEnabled)
        {
            Logger.Debug(PipelineStepDiagnostics.PrettyPrint(registrations));
        }

        invoker = PipelineInvoker.Build(registrations, behaviors);
    }

    public Task Invoke(TContext context)
    {
        context.Extensions.Invoker = invoker;
        return context.Extensions.Invoker(context);
    }

    readonly Func<IBehaviorContext, Task> invoker;

    static readonly ILog Logger = LogManager.GetLogger<Pipeline<TContext>>();
}