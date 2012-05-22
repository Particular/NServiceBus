using System;
using NServiceBus;
using NServiceBus.Management.Retries;
using NServiceBus.Management.Retries.Helpers;

namespace MyServer
{
    public class BeforeConfigurationIsFinalized : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            // The default policy will defer the message 5*N (where N is number of retries) 10 times.
            // For this sample we changed that to retry faster and only 3 times

            SecondLevelRetries.RetryPolicy = (tm) =>
                                                 {
                                                     if (TransportMessageHelpers.GetNumberOfRetries(tm) >= 3)
                                                     {
                                                         // To send back a value less than zero tells the 
                                                         // SecondLevelRetry satellite not to retry this message
                                                         // anymore.
                                                         return TimeSpan.MinValue;
                                                     }
                                                     // We will defer this message for 5 seconds, then send it back to the input queue (retry it)
                                                     return TimeSpan.FromSeconds(5);
                                                 };

            SecondLevelRetries.TimeoutPolicy = (tm) =>
                                                   {
                                                       // This is used as a last chance to abort the retry logic. 
                                                       // Retry according to the RetryPolicy Func, but not longer than this.                                                       
                                                       // In this case, we're just returning false, the retry timeout for this message never times out.
                                                       return false;
                                                   };
        }
    }
}