namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;

class PipelineModifications
{
    public List<RegisterStep> Additions = [];
    public List<ReplaceStep> Replacements = [];
    public List<RegisterOrReplaceStep> AdditionsOrReplacements = [];

    internal void AddAddition(RegisterStep step)
    {
        step.RegistrationOrder = GetNextInsertionOrder();
        Additions.Add(step);
    }

    internal void AddReplacement(ReplaceStep step)
    {
        step.RegistrationOrder = GetNextInsertionOrder();
        Replacements.Add(step);
    }

    internal void AddAdditionOrReplacement(RegisterOrReplaceStep step)
    {
        var order = GetNextInsertionOrder();
        step.RegisterStep.RegistrationOrder = order;
        step.ReplaceStep.RegistrationOrder = order;

        AdditionsOrReplacements.Add(step);
    }

    int GetNextInsertionOrder()
    {
        nextInsertionOrder++;
        return nextInsertionOrder;
    }

    int nextInsertionOrder;
}
