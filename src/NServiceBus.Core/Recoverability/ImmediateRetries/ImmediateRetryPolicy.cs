namespace NServiceBus
{
    class ImmediateRetryPolicy
    {
        public ImmediateRetryPolicy(int maxRetries)
        {
            this.maxRetries = maxRetries;
        }

        public bool ShouldGiveUp(int numberOfRetries)
        {
            return numberOfRetries >= maxRetries;
        }

        int maxRetries;
    }
}