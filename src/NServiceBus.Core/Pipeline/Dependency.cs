namespace NServiceBus.Pipeline
{
    class Dependency
    {
        public string Id { get; private set; }
        public bool Enforce { get; private set; }

        public Dependency(string id, bool enforce)
        {
            Id = id;
            Enforce = enforce;
        }
    }
}