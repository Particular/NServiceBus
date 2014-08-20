namespace NServiceBus.Core.Tests.Satellite
{
    using System;
    using System.Reflection;
    using System.Transactions;
    using Fakes;
    using Faults;
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

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.EndpointName("xyz");
            configurationBuilder.AssembliesToScan(new Assembly[0]);

            Transport = new TransportReceiver(new TransactionSettings(true, TimeSpan.FromSeconds(30), IsolationLevel.ReadCommitted, 5, false, false), 1, 0, FakeReceiver, InMemoryFaultManager, new SettingsHolder(), configurationBuilder.BuildConfiguration());

            RegisterTypes();
            Builder.Register<IManageMessageFailures>(() => InMemoryFaultManager);
            Builder.Register<TransportReceiver>(() => Transport);

            //var configurer = new SatelliteConfigurer();
            //configurer.Customize(configure);

            var launcher = new SatelliteLauncher(Builder);

            BeforeRun();
            launcher.Start();
        }

        public abstract void BeforeRun();
        public abstract void RegisterTypes();
    }
}
