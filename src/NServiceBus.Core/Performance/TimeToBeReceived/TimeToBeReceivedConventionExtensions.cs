namespace NServiceBus
{
    using System;
    using NServiceBus.Performance.TimeToBeReceived;

    /// <summary>
    /// Provides the ability to specify TTBR using a convention.
    /// </summary>
    public static class TimeToBeReceivedConventionExtensions
    {
        /// <summary>
        ///     Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        public static ConventionsBuilder DefiningTimeToBeReceivedAs(this ConventionsBuilder builder, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            Guard.AgainstNull(builder, "builder");
            Guard.AgainstNull(retrieveTimeToBeReceived, "retrieveTimeToBeReceived");
            
            builder.Settings.Set<UserDefinedTimeToBeReceivedConvention>(new UserDefinedTimeToBeReceivedConvention(retrieveTimeToBeReceived)); 

            return builder;
        }
    }
}