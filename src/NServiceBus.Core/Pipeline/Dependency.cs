#nullable enable

namespace NServiceBus;

class Dependency(string dependentId, string dependsOnId, Dependency.DependencyDirection direction, bool enforce)
{
    public enum DependencyDirection
    {
        Before = 1,
        After = 2
    }

    public string DependentId { get; } = dependentId;
    public string DependsOnId { get; } = dependsOnId;
    public bool Enforce { get; } = enforce;

    public DependencyDirection Direction { get; } = direction;
}