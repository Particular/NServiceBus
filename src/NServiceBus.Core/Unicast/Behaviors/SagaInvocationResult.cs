namespace NServiceBus
{

    class SagaInvocationResult
    {
        State state;

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

        public bool WasFound
        {
            get { return state != State.SagaNotFound; }
        }

        enum State
        {
            Unknown,
            SagaFound,
            SagaNotFound
        }
    }
}