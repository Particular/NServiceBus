﻿namespace NServiceBus.Core.Tests.Satellite
{
    using System.Reflection;
    using Fakes;
    using Faults;
    using NServiceBus.Config;
    using NUnit.Framework;
    using Satellites;
    using Unicast.Transport;

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

            Transport = new TransportReceiver
                {
                    Receiver = FakeReceiver,
                    TransactionSettings = TransactionSettings.Default
                };

            Configure.With(new Assembly[0])
                .DefineEndpointName("Test")
                .DefaultBuilder();
            Configure.Instance.Builder = Builder;
           
            RegisterTypes();
            Builder.Register<IManageMessageFailures>(() => InMemoryFaultManager);
            Builder.Register<TransportReceiver>(() => Transport);

            var configurer = new SatelliteConfigurer();
            configurer.Init();

            var launcher = new SatelliteLauncher();

            BeforeRun();
            launcher.Start();
        }

        public abstract void BeforeRun();
        public abstract void RegisterTypes();
    }
}
