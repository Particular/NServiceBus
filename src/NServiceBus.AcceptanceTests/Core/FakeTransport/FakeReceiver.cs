using System.Threading;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeReceiver : IMessageReceiver
    {
        public FakeReceiver(FakeTransport settings, Action<string, Exception> criticalErrorAction, string id)
        {
            this.settings = settings;
            this.criticalErrorAction = criticalErrorAction;
            Id = id;
        }


        public Task StartReceive(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CancellationToken cancellationToken)
        {
            settings.StartUpSequence.Add($"{nameof(IMessageReceiver)}.{nameof(StartReceive)}");

            if (settings.RaiseCriticalErrorDuringStartup)
            {
                criticalErrorAction(settings.ExceptionToThrow.Message, settings.ExceptionToThrow);
            }

            return Task.CompletedTask;
        }

        public async Task StopReceive(CancellationToken cancellationToken)
        {
            settings.StartUpSequence.Add($"{nameof(IMessageReceiver)}.{nameof(StopReceive)}");

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