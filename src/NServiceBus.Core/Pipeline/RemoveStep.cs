namespace NServiceBus.Pipeline
{
    class RemoveStep
    {
        public RemoveStep(string removeId)
        {
            RemoveId = removeId;
        }

        public string RemoveId { get; private set; }
    }
}