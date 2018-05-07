namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Janitor;
    using ObjectBuilder;
    using Pipeline;

    [SkipWeaving]
    class Pipeline<TContext> : IPipeline<TContext>
        where TContext : IBehaviorContext
    {
        public Pipeline(IBuilder builder, PipelineModifications pipelineModifications)
        {
            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);

            foreach (var rego in pipelineModifications.Additions)
            {
                coordinator.Register(rego);
            }

            // Important to keep a reference
            behaviors = coordinator.BuildPipelineModelFor<TContext>()
                .Select(r => r.CreateBehavior(builder)).ToArray();

            pipeline = behaviors.CreatePipelineExecutionFuncFor<TContext>();
        }

        public Task Invoke(TContext context)
        {
            return pipeline(context);
        }

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        IBehavior[] behaviors;
        Func<TContext, Task> pipeline;
    }
}