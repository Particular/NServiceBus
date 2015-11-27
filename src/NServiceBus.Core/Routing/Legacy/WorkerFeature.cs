﻿

namespace NServiceBus.Routing.Legacy
{
    using System;
    using System.Collections.Generic;
    using Features;
    using Transports;

    class WorkerFeature : Feature
    {
        internal WorkerFeature()
        {
            RegisterStartupTask<ReadyMessageSender>();
            Defaults(s =>
            {
                if (s.GetOrDefault<string>("PublicReturnAddress") != null)
                {
                    throw new Exception("We detected you have overridden the public return address with BusConfiguration. In order to enlist with a legacy distributor you need to remove this override as the public address needs to be set to the distributor address.");
                }
                s.Set("PublicReturnAddress", s.Get<string>("LegacyDistributor.Address"));
            });
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            var masterNodeControlAddress = context.Settings.Get<string>("LegacyDistributor.ControlAddress");
            var capacity = context.Settings.Get<int>("LegacyDistributor.Capacity");

            context.Container.ConfigureComponent(b => new ReadyMessageSender(b.Build<IDispatchMessages>(), context.Settings.LocalAddress(), capacity, masterNodeControlAddress), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new ProcessedMessageCounterBehavior(b.Build<ReadyMessageSender>()), DependencyLifecycle.SingleInstance);

            context.Pipeline.Register("ProcessedMessageCounterBehavior", typeof(ProcessedMessageCounterBehavior), "Counts messages processed by the worker.");

            return FeatureStartupTask.None;
        }
    }
}
