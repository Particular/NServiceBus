namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transports;
    using CriticalError = NServiceBus.CriticalError;

    class FakeReceiver : IPushMessages
    {
        public FakeReceiver(Exception throwCritical)
        {
            this.throwCritical = throwCritical;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task> onError, CriticalError onCriticalError, PushSettings settings)
        {
            criticalError = onCriticalError;
            return Task.FromResult(0);
        }

        public void Start(PushRuntimeSettings limitations)
        {
            if (throwCritical != null)
            {
                criticalError.Raise(throwCritical.Message, throwCritical);
            }
        }

        public Task Stop()
        {
            return Task.FromResult(0);
        }

        CriticalError criticalError;
        Exception throwCritical;
    }
}