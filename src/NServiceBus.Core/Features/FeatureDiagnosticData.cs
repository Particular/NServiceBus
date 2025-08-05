#nullable enable

namespace NServiceBus.Features;

using System.Collections.Generic;

class FeatureDiagnosticData
{
    public required string Name { get; internal init; }

    public bool EnabledByDefault { get; internal set; }

    public bool Active { get; internal set; }

    public required PrerequisiteStatus PrerequisiteStatus { get; internal set; }

    public required IReadOnlyList<IReadOnlyList<string>> Dependencies { get; internal set; }

    public required string Version { get; internal set; }

    public required IReadOnlyList<string> StartupTasks { get; internal set; }

    public bool DependenciesAreMet { get; internal set; }
}