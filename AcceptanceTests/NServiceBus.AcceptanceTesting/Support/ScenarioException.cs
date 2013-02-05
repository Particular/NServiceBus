namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Runtime.Serialization;

    public class ScenarioException : Exception
    {
        public ScenarioException()
        {
        }

        public ScenarioException(string message)
            : base(message)
        {
        }

        public ScenarioException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ScenarioException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}