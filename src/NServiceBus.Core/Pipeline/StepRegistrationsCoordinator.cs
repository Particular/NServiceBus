namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class StepRegistrationsCoordinator
    {
        public StepRegistrationsCoordinator(List<RemoveStep> removals, List<ReplaceBehavior> replacements)
        {
            this.removals = removals;
            this.replacements = replacements;
        }

        public void Register(string pipelineStep, Type behavior, string description)
        {
            additions.Add(RegisterStep.Create(pipelineStep, behavior, description, false));
        }

        public void Register(RegisterStep rego)
        {
            additions.Add(rego);
        }

        public IList<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext:BehaviorContext
        {
            var relevantAdditions = additions.Where(addition => typeof(TRootContext).IsAssignableFrom(GetInputType(addition.BehaviorType))).ToList();
            var relevantRemovals = removals.Where(removal => relevantAdditions.Any(a=>a.StepId == removal.RemoveId)).ToList();
            var relevantReplacements = replacements.Where(removal => relevantAdditions.Any(a => a.StepId == removal.ReplaceId)).ToList();


            var piplineModelBuilder = new PipelineModelBuilder(typeof(TRootContext),relevantAdditions, relevantRemovals, relevantReplacements);


            return piplineModelBuilder.Build();
        }

        static Type GetInputType(Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[0];
        }

        static Type GetBehaviorInterface(Type behaviorType)
        {
            var behaviorInterface = behaviorType.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            return behaviorInterface;
        }

        List<RegisterStep> additions = new List<RegisterStep>();
        List<RemoveStep> removals;
        List<ReplaceBehavior> replacements;
    }
}