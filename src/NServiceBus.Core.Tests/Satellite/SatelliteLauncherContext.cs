﻿namespace NServiceBus.Core.Tests.Satellite
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

            Transport = new TransportReceiver(new TransactionSettings(true, TimeSpan.FromSeconds(30), IsolationLevel.ReadCommitted, 5, false,false), 1, 0, FakeReceiver, InMemoryFaultManager, new SettingsHolder());

            Configure.With(o =>
            {
                o.EndpointName("xyz");
                o.AssembliesToScan(new Assembly[0]);
            });

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
