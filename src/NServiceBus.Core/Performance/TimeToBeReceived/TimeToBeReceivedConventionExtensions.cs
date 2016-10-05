namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides the ability to specify TTBR using a convention.
    /// </summary>
    public static class TimeToBeReceivedConventionExtensions
    {
        /// <summary>
        /// Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        public static ConventionsBuilder DefiningTimeToBeReceivedAs(this ConventionsBuilder builder, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            Guard.AgainstNull(nameof(builder), builder);
            Guard.AgainstNull(nameof(retrieveTimeToBeReceived), retrieveTimeToBeReceived);

            builder.Settings.Set<UserDefinedTimeToBeReceivedConvention>(new UserDefinedTimeToBeReceivedConvention(retrieveTimeToBeReceived));

            return builder;
        }
    }
}