namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides information about the fault configuration.
    /// </summary>
    public class FailedConfig
    {
        /// <summary>
        /// Creates a new fault configuration.
        /// </summary>
        /// <param name="errorQueue">The address of the error queue.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "FailedConfig(string errorQueue, HashSet<Type> unrecoverableExceptionTypes)", RemoveInVersion = "8.0", TreatAsErrorFromVersion = "7.0")]
        public FailedConfig(string errorQueue)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);

            ErrorQueue = errorQueue;
            UnrecoverableExceptionTypes = new HashSet<Type>();
        }

        /// <summary>
        /// Creates a new fault configuration.
        /// </summary>
        /// <param name="errorQueue">The address of the error queue.</param>
        /// <param name="unrecoverableExceptionTypes">Exception types that will be treated as unrecoverable.</param>
        public FailedConfig(string errorQueue, HashSet<Type> unrecoverableExceptionTypes)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);

            ErrorQueue = errorQueue;
            UnrecoverableExceptionTypes = unrecoverableExceptionTypes;
        }

        /// <summary>
        /// Gets the configured standard error queue.
        /// </summary>
        public string ErrorQueue { get; }

        /// <summary>
        /// Gets the exception types that will be treated as unrecoverable.
        /// </summary>
        public HashSet<Type> UnrecoverableExceptionTypes { get; }
    }
}