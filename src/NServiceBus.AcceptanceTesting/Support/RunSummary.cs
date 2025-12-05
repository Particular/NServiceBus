namespace NServiceBus.AcceptanceTesting.Support;

using System.Collections.Generic;

public class RunSummary
{
    public required RunResult Result { get; init; }

    public required RunDescriptor RunDescriptor { get; set; }

    public required IReadOnlyCollection<IComponentBehavior> Endpoints { get; set; }
}