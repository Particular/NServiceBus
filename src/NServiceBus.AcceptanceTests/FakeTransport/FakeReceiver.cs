namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    class FakeReceiver : IPushMessages
    {
        public FakeTransportContext Context { get; set; }

        public DequeueInfo Init(Func<PushContext, Task> pipe, PushSettings settings)
        {
            isMain = !settings.InputQueue.Contains("#");

            if (isMain)
            {
                Context.PushSettings = settings;
            }

            return new DequeueInfo("fake");
        }

        public void Start(PushRuntimeSettings limitations)
        {
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
    }
}