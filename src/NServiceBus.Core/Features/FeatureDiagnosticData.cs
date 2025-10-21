#nullable enable

namespace NServiceBus.Features;

using System.Collections.Generic;

class FeatureDiagnosticData
{
    public required string Name { get; set; }

    public bool EnabledByDefault { get; set; }

    public bool Active { get; set; }

    public required PrerequisiteStatus PrerequisiteStatus { get; set; }

    public required IReadOnlyList<string> Dependencies { get; set; }

    public required string Version { get; set; }

    public required IReadOnlyList<string> StartupTasks { get; set; }

    public bool DependenciesAreMet { get; set; }
}