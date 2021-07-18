namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class SagaInvocationResult
    {
        public void SagaFound(Type type)
        {
            Results[type] = State.SagaFound;
        }

        public void SagaNotFound(Type type)
        {
            Results[type] = State.SagaNotFound;
        }

        public Dictionary<Type, State> Results { get; set; } = new Dictionary<Type, State>();

        public enum State
        {
            SagaFound,
            SagaNotFound
        }
    }
}