namespace NServiceBus.Pipeline
{
    class RemoveBehavior
    {
        public RemoveBehavior(string removeId)
        {
            RemoveId = removeId;
        }

        public string RemoveId { get; private set; }
    }
}