namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;

class PipelineModifications
{
    public IReadOnlyCollection<RegisterStep> Additions => additions;
    public IReadOnlyCollection<ReplaceStep> Replacements => replacements;
    public IReadOnlyCollection<RegisterOrReplaceStep> AdditionsOrReplacements => additionsOrReplacements;

    internal void AddAddition(RegisterStep step)
    {
        step.RegistrationOrder = GetNextInsertionOrder();
        additions.Add(step);
    }

    internal void AddReplacement(ReplaceStep step)
    {
        step.RegistrationOrder = GetNextInsertionOrder();
        replacements.Add(step);
    }

    internal void AddAdditionOrReplacement(RegisterOrReplaceStep step)
    {
        var order = GetNextInsertionOrder();
        step.RegisterStep.RegistrationOrder = order;
        step.ReplaceStep.RegistrationOrder = order;

        additionsOrReplacements.Add(step);
    }

    int GetNextInsertionOrder()
    {
        nextInsertionOrder++;
        return nextInsertionOrder;
    }

    int nextInsertionOrder;
    readonly List<RegisterStep> additions = new List<RegisterStep>();
    readonly List<ReplaceStep> replacements = new List<ReplaceStep>();
    readonly List<RegisterOrReplaceStep> additionsOrReplacements = new List<RegisterOrReplaceStep>();
}