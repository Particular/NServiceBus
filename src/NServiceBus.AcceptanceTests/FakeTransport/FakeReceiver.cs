namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transports;

    class FakeReceiver : IPushMessages
    {
        public FakeReceiver(Exception throwCritical)
        {
            this.throwCritical = throwCritical;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError, Func<string, Exception, Task> onCriticalError, PushSettings settings)
        {
            criticalError = onCriticalError;
            return Task.FromResult(0);
        }

        public Task Start(PushRuntimeSettings limitations)
        {
            if (throwCritical != null)
            {
                criticalError(throwCritical.Message, throwCritical);
            }

            return Task.FromResult(0);
        }

        public Task Stop()
        {
            return Task.FromResult(0);
        }

        Func<string, Exception, Task> criticalError;
        Exception throwCritical;
    }
}