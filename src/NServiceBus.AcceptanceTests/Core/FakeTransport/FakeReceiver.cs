namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeReceiver : IPushMessages
    {
        public FakeReceiver(FakeTransport settings)
        {
            this.settings = settings;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, NServiceBus.CriticalError criticalError, PushSettings pushSettings)
        {
            settings.StartUpSequence.Add($"{nameof(IPushMessages)}.{nameof(Init)}");

            this.criticalError = criticalError;
            return Task.FromResult(0);
        }

        public void Start(PushRuntimeSettings limitations)
        {
            settings.StartUpSequence.Add($"{nameof(IPushMessages)}.{nameof(Start)}");

            if (settings.RaiseCriticalErrorDuringStartup)
            {
                criticalError.Raise(settings.ExceptionToThrow.Message, settings.ExceptionToThrow);
            }
        }

        public async Task Stop()
        {
            settings.StartUpSequence.Add($"{nameof(IPushMessages)}.{nameof(Stop)}");

            await Task.Yield();

            if (settings.ThrowOnPumpStop)
            {
                throw settings.ExceptionToThrow;
            }
        }

        FakeTransport settings;
        NServiceBus.CriticalError criticalError;
    }
}