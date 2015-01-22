namespace NServiceBus.FirstLevelRetries
{
    class FirstLevelRetryPolicy
    {
        readonly int maxRetries;

        public FirstLevelRetryPolicy(int maxRetries)
        {
            this.maxRetries = maxRetries;
        }

        public bool ShouldGiveUp(int numberOfRetries)
        {
            return numberOfRetries >= maxRetries;
        }
    }
}