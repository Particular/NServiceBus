namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeReceiver : IPushMessages
    {
        public FakeReceiver(FakeTransport settings, Action<string, Exception> criticalErrorAction)
        {
            this.settings = settings;
            this.criticalErrorAction = criticalErrorAction;
        }


        public void Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
        {
            settings.StartUpSequence.Add($"{nameof(IPushMessages)}.{nameof(Start)}");

            if (settings.RaiseCriticalErrorDuringStartup)
            {
                criticalErrorAction(settings.ExceptionToThrow.Message, settings.ExceptionToThrow);
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

        public IManageSubscriptions Subscriptions { get; }

        FakeTransport settings;
        private readonly Action<string, Exception> criticalErrorAction;
    }
}