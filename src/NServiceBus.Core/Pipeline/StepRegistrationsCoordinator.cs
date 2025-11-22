namespace NServiceBus;

using System;
using System.Collections.Generic;
using Pipeline;

class StepRegistrationsCoordinator
{
    public StepRegistrationsCoordinator(List<ReplaceStep> replacements, List<RegisterOrReplaceStep> addOrReplaceSteps)
    {
        this.replacements = replacements;
        this.addOrReplaceSteps = addOrReplaceSteps;
    }

    public void Register(string pipelineStep, Type behavior, string description)
    {
        var registerStep = RegisterStep.Create(pipelineStep, behavior, description);
        registerStep.RegistrationOrder = GetNextInsertionOrder();

        additions.Add(registerStep);
    }

    public void Register(RegisterStep rego)
    {
        rego.RegistrationOrder = GetNextInsertionOrder();

        additions.Add(rego);
    }

    public List<RegisterStep> BuildPipelineModelFor<TRootContext>() where TRootContext : IBehaviorContext
    {
        var pipelineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), additions, replacements, addOrReplaceSteps);
        return pipelineModelBuilder.Build();
    }

    readonly List<RegisterStep> additions = [];
    readonly List<ReplaceStep> replacements;
    readonly List<RegisterOrReplaceStep> addOrReplaceSteps;

    int nextInsertionOrder;

    int GetNextInsertionOrder()
    {
        nextInsertionOrder++;
        return nextInsertionOrder;
    }
}
