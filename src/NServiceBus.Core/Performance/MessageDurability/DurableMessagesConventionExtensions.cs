namespace NServiceBus
{
    using System;

    /// <summary>
    /// Add extensions to allow conventions for message durability to be changed.
    /// </summary>
    public static class DurableMessagesConventionExtensions
    {
        /// <summary>
        /// Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        public static ConventionsBuilder DefiningExpressMessagesAs(this ConventionsBuilder builder, Func<Type, bool> definesExpressMessageType)
        {
            Guard.AgainstNull(nameof(builder), builder);
            Guard.AgainstNull(nameof(definesExpressMessageType), definesExpressMessageType);

            builder.Settings.Set("messageDurabilityConvention", definesExpressMessageType);

            return builder;
        }
    }
}