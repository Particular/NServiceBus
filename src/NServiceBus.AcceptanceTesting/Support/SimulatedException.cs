namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// A dummy exception to be used in acceptance tests for easier differentiation from real exceptions.
    /// </summary>
    public class SimulatedException : Exception
    {
        public SimulatedException()
        {
        }

        public SimulatedException(string message) : base(message)
        {
        }

        public SimulatedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SimulatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}