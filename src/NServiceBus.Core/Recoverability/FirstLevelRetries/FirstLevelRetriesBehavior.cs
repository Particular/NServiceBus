namespace NServiceBus
{
    using System;

    class FirstLevelRetriesBehavior
    {
        public FirstLevelRetriesBehavior(FirstLevelRetryPolicy retryPolicy)
        {
        }

        public bool Invoke(Exception exception, int firstLevelRetries)
        {
            throw new NotImplementedException();
        }

    }
}