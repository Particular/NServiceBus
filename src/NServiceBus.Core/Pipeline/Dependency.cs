namespace NServiceBus
{
    class Dependency
    {
        public string DependantId { get; private set; }
        public string DependsOnId { get; private set; }
        public bool Enforce { get; private set; }

        public DependencyDirection Direction{ get; private set; }

        public Dependency(string dependantId, string dependsOnId, DependencyDirection direction, bool enforce)
        {
            DependantId = dependantId;
            DependsOnId = dependsOnId;
            Direction = direction;
            Enforce = enforce;
        }

        public enum DependencyDirection
        {
            Before = 1,
            After = 2
        }
    }
}