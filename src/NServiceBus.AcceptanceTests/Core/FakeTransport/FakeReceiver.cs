namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    class FakeReceiver : IPushMessages
    {
        public FakeReceiver(ReadOnlySettings settings)
        {
            this.settings = settings;

            throwCritical = settings.GetOrDefault<bool>("FakeTransport.ThrowCritical");
            throwOnStop = settings.GetOrDefault<bool>("FakeTransport.ThrowOnPumpStop");

            exceptionToThrow = settings.GetOrDefault<Exception>();
        }

        public Task Init(Func<MessageContext, CancellationToken, Task> onMessage, Func<ErrorContext, CancellationToken, Task<ErrorHandleResult>> onError, NServiceBus.CriticalError criticalError, PushSettings pushSettings, CancellationToken cancellationToken)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(IPushMessages)}.{nameof(Init)}");

            this.criticalError = criticalError;
            return Task.FromResult(0);
        }

        public void Start(PushRuntimeSettings limitations, CancellationToken cancellationToken)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(IPushMessages)}.{nameof(Start)}");

            if (throwCritical)
            {
                criticalError.Raise(exceptionToThrow.Message, exceptionToThrow);
            }
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(IPushMessages)}.{nameof(Stop)}");

            await Task.Yield();

            if (throwOnStop)
            {
                throw exceptionToThrow;
            }
        }

        ReadOnlySettings settings;
        NServiceBus.CriticalError criticalError;
        bool throwCritical;
        bool throwOnStop;
        Exception exceptionToThrow;
    }
}