namespace NServiceBus
{
    using System;

    class FirstLevelRetriesBehavior
    {
        public FirstLevelRetriesBehavior(FirstLevelRetryPolicy retryPolicy)
        {
            this.retryPolicy = retryPolicy;
        }

        public bool Invoke(Exception exception, int firstLevelRetries)
        {
          
        }

    }
}