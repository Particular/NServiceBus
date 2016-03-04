namespace NServiceBus
{
    class FirstLevelRetryPolicy
    {
        public FirstLevelRetryPolicy(int maxRetries)
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