namespace NServiceBus
{
    using System;

    /// <summary>
    /// Used to throw an exception cannot be handled by recoverability.
    /// Added by default to <see cref="RecoverabilitySettings.AddUnrecoverableException"/>.
    /// </summary>
    [Serializable]
    public class UnrecoverableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnrecoverableException" />.
        /// </summary>
        public UnrecoverableException()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UnrecoverableException" />.
        /// </summary>
        public UnrecoverableException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UnrecoverableException" />.
        /// </summary>
        public UnrecoverableException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}