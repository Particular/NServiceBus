using System;
using NServiceBus.Config;
using NServiceBus.Management.Retries;
using NServiceBus.Management.Retries.Helpers;

namespace MyServer
{
    using NServiceBus;

    public class ChangeRetryPolicy : INeedInitialization
    {
        public void Init()
        {
            // The default policy will defer the message 10*N (where N is number of retries) seconds 3 times. (60 sec total)
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

                                                     // If an exception is thrown within SLR, we should send the message
                                                     // direct to the error queue since we can't do anything with it.
                                                     // if (TransportMessageHelpers.GetNumberOfRetries(tm) == 1)
                                                     // {
                                                     //     throw new Exception("This exception it thrown from SLR, message should go to error queue");
                                                     // }

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