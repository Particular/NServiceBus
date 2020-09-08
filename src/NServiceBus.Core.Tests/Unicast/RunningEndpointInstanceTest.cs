namespace NServiceBus.Unicast.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RunningEndpointInstanceTest
    {
        [Test]
        public async Task ShouldAllowMultipleStops()
        {
            var settings = new SettingsHolder();

            var testee = new RunningEndpointInstance(
                settings,
                new HostingComponent(null),
                null,
                new FeatureComponent(settings),
                new MessageSession(new FakeRootContext()),
                null);

            await testee.Stop(CancellationToken.None);

            Assert.That(async () => await testee.Stop(CancellationToken.None), Throws.Nothing);
        }
        
        // TODO: we probably want to add tests here
    }
}