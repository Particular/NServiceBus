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
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                throw new ArgumentException("Exception type must be an exception", nameof(exceptionType));
            }

            HashSet<Type> unrecoverableExceptions;
            if (!settings.TryGet(Recoverability.UnrecoverableExceptions, out unrecoverableExceptions))
            {
                unrecoverableExceptions = new HashSet<Type> { typeof(MessageDeserializationException) };
                settings.Set(Recoverability.UnrecoverableExceptions, unrecoverableExceptions);
            }

            unrecoverableExceptions.Add(exceptionType);
        }
    }
}