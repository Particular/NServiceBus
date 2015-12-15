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
                new FuncBuilder(), 
                new PipelineCollection(Enumerable.Empty<TransportReceiver>()), 
                new StartAndStoppablesRunner(Enumerable.Empty<IWantToRunWhenBusStartsAndStops>()), 
                new FeatureRunner(new FeatureActivator(new SettingsHolder())), 
                new FakeSessionFactory());

            await testee.Stop();

            Assert.DoesNotThrow(async () => await testee.Stop());
        }

        class FakeSessionFactory : IBusSessionFactory
        {
            public IBusSession CreateBusSession()
            {
                return new BusSession(new RootContext(null));
            }
        }
    }
}