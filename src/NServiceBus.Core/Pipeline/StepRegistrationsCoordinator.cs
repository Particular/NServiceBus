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

        public List<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext : IBehaviorContext
        {
            // var relevantRemovals = removals.Where(removal => additions.Any(a => a.StepId == removal.RemoveId)).ToList();
            // var relevantReplacements = replacements.Where(replacement => additions.Any(a => a.StepId == replacement.ReplaceId)).ToList();
            //
            // var irrelevantReplacementsThatShouldFail = replacements
            //     .Where(registeredReplacement => relevantReplacements.All(relevantReplacement => relevantReplacement.ReplaceId != registeredReplacement.ReplaceId))
            //     //.Where(replacement => replacement.FailIfStepNotFound)
            //     .ToList();
            //
            // if (irrelevantReplacementsThatShouldFail.Any())
            // {
            //     var replaceIdentifiers = irrelevantReplacementsThatShouldFail.Select(x => x.ReplaceId);
            //     var pipelineIdentifiersNotFound = string.Join(",", replaceIdentifiers);
            //     throw new InvalidOperationException($"Pipeline replacements were registered for the following ID's: {pipelineIdentifiersNotFound}. These could not be found in the pipeline and could therefore not be replaced. Please verify if the correct ID was used");
            // }

            var pipelineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), additions, removals, replacements);
            return pipelineModelBuilder.Build();
        }

        List<RegisterStep> additions = new List<RegisterStep>();
        List<RemoveStep> removals;
        List<ReplaceStep> replacements;
    }
}