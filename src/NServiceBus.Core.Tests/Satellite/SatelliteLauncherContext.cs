namespace NServiceBus.Core.Tests.Satellite
{
    using System.Reflection;
    using Fakes;
    using Faults;
    using NServiceBus.Config;
    using NUnit.Framework;
    using Satellites;
    using Settings;
    using Unicast.Transport;
    using TransactionSettings = Unicast.Transport.TransactionSettings;

    public abstract class SatelliteLauncherContext
    {
        protected FuncBuilder Builder;
        protected IManageMessageFailures InMemoryFaultManager;
        protected TransportReceiver Transport;
        protected FakeReceiver FakeReceiver;

        [SetUp]
        public void SetUp()
        {
            Builder = new FuncBuilder();
            InMemoryFaultManager = new Faults.InMemory.FaultManager();
            FakeReceiver = new FakeReceiver();

            Transport = new TransportReceiver(TransactionSettings.Default, 1, 0, FakeReceiver, InMemoryFaultManager, new SettingsHolder());

            Configure.With(o =>
            {
                o.EndpointName("xyz");
                o.AssembliesToScan(new Assembly[0]);
            });

            RegisterTypes();
            Builder.Register<IManageMessageFailures>(() => InMemoryFaultManager);
            Builder.Register<TransportReceiver>(() => Transport);

            var configurer = new SatelliteConfigurer();
            configurer.Init(Configure.Instance);

            var launcher = new SatelliteLauncher(Builder);

            BeforeRun();
            launcher.Start();
        }

        public abstract void BeforeRun();
        public abstract void RegisterTypes();
    }
}
