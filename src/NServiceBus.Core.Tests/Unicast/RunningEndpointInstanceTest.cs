namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RunningEndpointInstanceTest
    {
        static RunningEndpointInstance Create()
        {
            var settings = new SettingsHolder();

            var testInstance = new RunningEndpointInstance(
                settings,
                new HostingComponent(null, true),
                null,
                new FeatureComponent(settings),
                new MessageSession(new FakeRootContext()),
                null);
            return testInstance;
        }

        [Test]
        public async Task ShouldAllowMultipleStops()
        {
            var testInstance = Create();

            await testInstance.Stop();

            Assert.That(async () => await testInstance.Stop(), Throws.Nothing);
        }

        [Test]
        public async Task ShouldThrowExceptionAfterInvokingStop()
        {
            var testInstance = Create();

            await testInstance.Stop();

            Assert.Throws<InvalidOperationException>(() => testInstance.Send(new object(), new SendOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
            Assert.Throws<InvalidOperationException>(() => testInstance.Send<object>(_=> { }, new SendOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
            Assert.Throws<InvalidOperationException>(() => testInstance.Publish(new object(), new PublishOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
            Assert.Throws<InvalidOperationException>(() => testInstance.Publish<object>(_ => { }, new PublishOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
            Assert.Throws<InvalidOperationException>(() => testInstance.Subscribe(typeof(object), new SubscribeOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
            Assert.Throws<InvalidOperationException>(() => testInstance.Unsubscribe(typeof(object), new UnsubscribeOptions()), "Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        }
    }
}