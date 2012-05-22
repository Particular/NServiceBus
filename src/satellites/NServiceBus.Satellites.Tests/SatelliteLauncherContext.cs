using System.Reflection;
using NServiceBus.Config;
using NServiceBus.Faults;
using NServiceBus.Satellites.Config;
using NUnit.Framework;

namespace NServiceBus.Satellites.Tests
{
    public abstract class SatelliteLauncherContext
    {
        protected FuncBuilder Builder;
        protected IManageMessageFailures InMemoryFaultManager;
        protected FakeTransportBuilder TransportBuilder;
 
        [SetUp]
        public void SetUp()
        {
            Builder = new FuncBuilder();
            InMemoryFaultManager = new NServiceBus.Faults.InMemory.FaultManager();
            TransportBuilder = new FakeTransportBuilder();

            Configure.With(new Assembly[0]);
            Configure.Instance.Builder = Builder;
           
            RegisterTypes();
            Builder.Register<IManageMessageFailures>(() => InMemoryFaultManager);

            var configurer = new SatelliteConfigurer();
            configurer.Init();
            //configurer.Run();

            var launcher = new NonThreadingSatelliteLauncher
                               {
                                   Builder = Builder,
                                   TransportBuilder = TransportBuilder
                               };

            BeforeRun();
            launcher.Run();
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