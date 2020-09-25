namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Pipeline;

    class StepRegistrationsCoordinator
    {
        public StepRegistrationsCoordinator(List<RemoveStep> removals, List<ReplaceStep> replacements, List<AddOrReplaceStep> addOrReplaceSteps)
        {
            this.removals = removals;
            this.replacements = replacements;
            this.addOrReplaceSteps = addOrReplaceSteps;
        }

        public void Register(string pipelineStep, Type behavior, string description)
        {
            additions.Add(RegisterStep.Create(pipelineStep, behavior, description));
        }

        public void Register(RegisterStep rego)
        {
            additions.Add(rego);
        }

        public List<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext : IBehaviorContext
        {
            var pipelineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), additions, removals, replacements, addOrReplaceSteps);
            return pipelineModelBuilder.Build();
        }

        List<RegisterStep> additions = new List<RegisterStep>();
        List<RemoveStep> removals;
        List<ReplaceStep> replacements;
        List<AddOrReplaceStep> addOrReplaceSteps;
    }
}