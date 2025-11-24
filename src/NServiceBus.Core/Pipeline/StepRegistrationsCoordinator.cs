namespace NServiceBus;

using System;
using System.Collections.Generic;
using Pipeline;

class StepRegistrationsCoordinator(List<ReplaceStep> replacements, List<RegisterOrReplaceStep> addOrReplaceSteps)
{
    public void Register(string pipelineStep, Type behavior, string description) => additions.Add(RegisterStep.Create(pipelineStep, behavior, description));

    public void Register(RegisterStep rego) => additions.Add(rego);

    public List<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext : IBehaviorContext
    {
        var pipelineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), additions, replacements, addOrReplaceSteps);
        return pipelineModelBuilder.Build();
    }

    readonly List<RegisterStep> additions = [];
}