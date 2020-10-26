using System.Threading;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeReceiver : IPushMessages
    {
        public FakeReceiver(FakeTransport settings, Action<string, Exception> criticalErrorAction, string id)
        {
            this.settings = settings;
            this.criticalErrorAction = criticalErrorAction;
            Id = id;
        }


        public Task Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CancellationToken cancellationToken)
        {
            settings.StartUpSequence.Add($"{nameof(IPushMessages)}.{nameof(Start)}");

            if (settings.RaiseCriticalErrorDuringStartup)
            {
                criticalErrorAction(settings.ExceptionToThrow.Message, settings.ExceptionToThrow);
            }

            return Task.CompletedTask;
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            settings.StartUpSequence.Add($"{nameof(IPushMessages)}.{nameof(Stop)}");

            await Task.Yield();

            if (settings.ThrowOnPumpStop)
            {
                throw settings.ExceptionToThrow;
            }
        }

        public IManageSubscriptions Subscriptions { get; }
        public string Id { get; }

        FakeTransport settings;
        private readonly Action<string, Exception> criticalErrorAction;
    }
}