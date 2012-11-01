using System.Reflection;
using NServiceBus.Config;
using NServiceBus.Faults;
using NUnit.Framework;

namespace NServiceBus.Satellites.Tests
{
    using Config;

    public abstract class SatelliteLauncherContext
    {
        protected FuncBuilder Builder;
        protected IManageMessageFailures InMemoryFaultManager;
        protected FakeTransportBuilder TransportBuilder;
 
        [SetUp]
        public void SetUp()
        {
            Builder = new FuncBuilder();
            InMemoryFaultManager = new Faults.InMemory.FaultManager();
            TransportBuilder = new FakeTransportBuilder();

            Configure.With(new Assembly[0])
                .DefineEndpointName("Test")
                .DefaultBuilder();
            Configure.Instance.Builder = Builder;
           
            RegisterTypes();
            Builder.Register<IManageMessageFailures>(() => InMemoryFaultManager);

            var configurer = new SatelliteConfigurer();
            configurer.Init();

            var launcher = new NonThreadingSatelliteLauncher
                               {
                                   Builder = Builder,
                                   TransportBuilder = TransportBuilder
                               };

            BeforeRun();
            launcher.Start();
        }

        public abstract void BeforeRun();
        public abstract void RegisterTypes();
    }

    public class NonThreadingSatelliteLauncher : SatelliteLauncher
    {
        protected override void StartSatellite(SatelliteContext ctx)
        {
            Execute(ctx);
        }
    }
}