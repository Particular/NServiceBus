namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Transports;

    public class FakeTransportContext : ScenarioContext
    {
        public PushSettings PushSettings { get; set; }
        public PushRuntimeSettings PushRuntimeSettings { get; set; }
    }
}