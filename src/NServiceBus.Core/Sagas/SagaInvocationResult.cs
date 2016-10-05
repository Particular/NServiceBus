namespace NServiceBus
{
    class SagaInvocationResult
    {
        public bool WasFound => state != State.SagaNotFound;

        public void SagaFound()
        {
            state = State.SagaFound;
        }

        public void SagaNotFound()
        {
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