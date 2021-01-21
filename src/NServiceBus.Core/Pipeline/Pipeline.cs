namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using Pipeline;

    [SkipWeaving]
    class Pipeline<TContext> : IPipeline<TContext>
        where TContext : IBehaviorContext
    {
        public Pipeline(IServiceProvider builder, PipelineModifications pipelineModifications)
        {
            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Replacements, pipelineModifications.AdditionsOrReplacements);

            foreach (var rego in pipelineModifications.Additions)
            {
                coordinator.Register(rego);
            }

            // Important to keep a reference
            behaviors = coordinator.BuildPipelineModelFor<TContext>()
                .Select(r => r.CreateBehavior(builder)).ToArray();

            pipeline = behaviors.CreatePipelineExecutionFuncFor<TContext>();
        }

        public Task Invoke(TContext context, CancellationToken token)
        {
            return pipeline(context, token);
        }

        IBehavior[] behaviors;
        Func<TContext, CancellationToken, Task> pipeline;
    }
}