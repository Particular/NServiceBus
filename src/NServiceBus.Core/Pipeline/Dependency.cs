namespace NServiceBus
{
    class Dependency
    {
        public enum DependencyDirection
        {
            Before = 1,
            After = 2
        }

        public Dependency(string dependentId, string dependsOnId, DependencyDirection direction, bool enforce)
        {
            DependentId = dependentId;
            DependsOnId = dependsOnId;
            Direction = direction;
            Enforce = enforce;
        }

        public string DependentId { get; private set; }
        public string DependsOnId { get; private set; }
        public bool Enforce { get; private set; }

        public DependencyDirection Direction { get; private set; }
    }
}