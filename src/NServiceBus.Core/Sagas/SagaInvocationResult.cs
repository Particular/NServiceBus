namespace NServiceBus
{
    using Sagas;

    class SagaInvocationResult
    {
        public SagaMetadata SagaMetadata { get; private set; }
        public bool WasFound => state != State.SagaNotFound;

        public void SagaFound()
        {
            state = State.SagaFound;
        }

        public void SagaNotFound(SagaMetadata sagaMetadata)
        {
            SagaMetadata = sagaMetadata;

            if (state == State.Unknown)
            {
                state = State.SagaNotFound;
            }
        }

        State state;

        enum State
        {
            Unknown,
            SagaFound,
            SagaNotFound
        }
    }
}