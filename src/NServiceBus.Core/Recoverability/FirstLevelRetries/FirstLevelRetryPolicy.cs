namespace NServiceBus.Recoverability.FirstLevelRetries
{
    class FirstLevelRetryPolicy
    {
        int maxRetries;

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