using NUnit.Framework;

namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Settings;
    using Transport;

    [TestFixture]
    public class When_starting_transport_outside_of_an_endpoint
    {
        [Test]
        public async Task Should_send_and_receive_messages()
        {
            var learningTransport = new LearningTransport();
            var diagnostics = new StartupDiagnosticEntries();
            var settings = new SettingsHolder();

            var transportInfrastructure = learningTransport.Initialize(
                "EndpointName", string.Empty, diagnostics, settings);

            var receiving = transportInfrastructure.ConfigureReceiveInfrastructure("LocalAddress");

            var receivePump = receiving.MessagePumpFactory();

            var messageReceived = false;

            await receivePump.Init(
                c =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                er => Task.FromResult(ErrorHandleResult.Handled),
                new CriticalError(context => Task.FromResult(0)),
                new PushSettings("input-queue", "error-queue", false, TransportTransactionMode.ReceiveOnly));

            receivePump.Start(new PushRuntimeSettings());

            var sending = transportInfrastructure.ConfigureSendInfrastructure();

            var dispatcher = sending.DispatcherFactory();

            await dispatcher.Dispatch(new TransportOperations(new[]
            {
                new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), new UnicastAddressTag("input-queue")),
            }), new TransportTransaction(), new ContextBag());

            var stopwatch = Stopwatch.StartNew();

            while (messageReceived == false && stopwatch.Elapsed < TimeSpan.FromMinutes(1))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Assert.IsTrue(messageReceived);
        }
    }
}