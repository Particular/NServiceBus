namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Pipeline;

    class StepRegistrationsCoordinator
    {
        public StepRegistrationsCoordinator(List<RemoveStep> removals, List<ReplaceStep> replacements)
        {
            this.removals = removals;
            this.replacements = replacements;
        }

        public void Register(string pipelineStep, Type behavior, string description)
        {
            additions.Add(RegisterStep.Create(pipelineStep, behavior, description));
        }

        public void Register(RegisterStep rego)
        {
            additions.Add(rego);
        }

        public IList<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext : IBehaviorContext
        {
            var relevantAdditions = additions.ToList();
            var relevantRemovals = removals.Where(removal => relevantAdditions.Any(a => a.StepId == removal.RemoveId)).ToList();
            var relevantReplacements = replacements.Where(removal => relevantAdditions.Any(a => a.StepId == removal.ReplaceId)).ToList();

            var piplineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), relevantAdditions, relevantRemovals, relevantReplacements);

            return piplineModelBuilder.Build();
        }

        List<RegisterStep> additions = new List<RegisterStep>();
        List<RemoveStep> removals;
        List<ReplaceStep> replacements;
    }
}