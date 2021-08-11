namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Settings;

    /// <summary>
    /// Provides extension methods for recoverability settings.
    /// </summary>
    public static class RecoverabilitySettingsExtensions
    {
        /// <summary>
        /// Adds the specified exception type to be treated as an unrecoverable exception.
        /// </summary>
        /// <param name="settings">The extended settings.</param>
        /// <param name="exceptionType">The exception type.</param>
        public static void AddUnrecoverableException(this SettingsHolder settings, Type exceptionType)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Guard.AgainstNull(nameof(exceptionType), exceptionType);

            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                throw new ArgumentException("Exception type must be an exception", nameof(exceptionType));
            }

            if (!settings.TryGet(RecoverabilityComponent.UnrecoverableExceptions, out HashSet<Type> unrecoverableExceptions))
            {
                unrecoverableExceptions = new HashSet<Type>();
                settings.Set(RecoverabilityComponent.UnrecoverableExceptions, unrecoverableExceptions);
            }

            unrecoverableExceptions.Add(exceptionType);
        }

        internal static HashSet<Type> UnrecoverableExceptions(this IReadOnlySettings settings)
        {
            return settings.GetOrDefault<HashSet<Type>>(RecoverabilityComponent.UnrecoverableExceptions) ?? new HashSet<Type>();
        }
    }
}