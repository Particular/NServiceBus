namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    class StepRegistrationsCoordinator
    {
        public StepRegistrationsCoordinator(
            IReadOnlyCollection<RegisterStep> additions,
            IReadOnlyCollection<ReplaceStep> replacements,
            IReadOnlyCollection<RegisterOrReplaceStep> addOrReplaceSteps)
        {
            this.additions = additions;
            this.replacements = replacements;
            this.addOrReplaceSteps = addOrReplaceSteps;
        }

        public IReadOnlyCollection<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext : IBehaviorContext
        {
            var pipelineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), additions, replacements, addOrReplaceSteps);
            return pipelineModelBuilder.Build();
        }

        readonly IReadOnlyCollection<RegisterStep> additions;
        readonly IReadOnlyCollection<ReplaceStep> replacements;
        readonly IReadOnlyCollection<RegisterOrReplaceStep> addOrReplaceSteps;
    }
}