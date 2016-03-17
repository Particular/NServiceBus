namespace NServiceBus.Unicast.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Core.Tests;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class RunningEndpointInstanceTest
    {
        [Test]
        public async Task ShouldAllowMultipleStops()
        {
            var testee = new RunningEndpointInstance(
                new SettingsHolder(), 
                new FuncBuilder(), 
                new PipelineCollection(Enumerable.Empty<TransportReceiver>()), 
                new StartAndStoppablesRunner(Enumerable.Empty<IWantToRunWhenBusStartsAndStops>()), 
                new FeatureRunner(new FeatureActivator(new SettingsHolder())),
                new MessageSession(new RootContext(null, null, null)),
                new ShutdownDelegateRegistry());

            await testee.Stop();

            Assert.That(async () => await testee.Stop(), Throws.Nothing);
        }
    }
}