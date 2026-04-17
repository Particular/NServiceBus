namespace NServiceBus.AcceptanceTests.Core.FakeTransport;

using NUnit.Framework;
using Transport;

static class FakeTransportAssertions
{
    extension(FakeTransport fakeTransport)
    {
        public void AssertDidNotStartReceivers() =>
            Assert.That(new[]
            {
                $"{nameof(TransportDefinition)}.{nameof(TransportDefinition.Initialize)}",
                $"{nameof(IMessageReceiver)}.{nameof(IMessageReceiver.Initialize)} for receiver Main",
            }, Is.EqualTo(fakeTransport.StartupSequence).AsCollection, "Should not start the receivers");
    }
}