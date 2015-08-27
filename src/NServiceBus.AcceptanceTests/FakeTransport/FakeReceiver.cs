namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using NServiceBus.Transports;

    class FakeReceiver : IPushMessages
    {
        public FakeTransportContext Context { get; set; }


        public DequeueInfo Init(Action<PushContext> pipe, PushSettings settings)
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

        public void Stop()
        {
        }

        bool isMain;
    }
}