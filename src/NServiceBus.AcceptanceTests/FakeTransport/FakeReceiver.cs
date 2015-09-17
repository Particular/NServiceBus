namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using CriticalError = NServiceBus.CriticalError;

    class FakeReceiver : IPushMessages
    {
        public FakeTransportContext Context { get; set; }
        public ReadOnlySettings Settings { get; set; }

        public CriticalError CriticalError { get; set; }

        public void Init(Func<PushContext, Task> pipe, PushSettings settings)
        {
            isMain = !settings.InputQueue.Contains("#");
            throwCritical = Settings.Get<Exception>("FakeTransport.RaiseCriticalErrorDuringStartup");

            if (isMain)
            {
                Context.PushSettings = settings;
            }
        }

        public void Start(PushRuntimeSettings limitations)
        {
            if (throwCritical != null)
            {
                CriticalError.Raise(throwCritical.Message, throwCritical);
            }

            if (isMain)
            {
                Context.PushRuntimeSettings = limitations;
            }
        }

        public Task Stop()
        {
            return Task.FromResult(0);
        }

        bool isMain;
        Exception throwCritical;
    }
}