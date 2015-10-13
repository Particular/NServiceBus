namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;

    internal class Sending : Feature
    {
        public Sending()
        {
            EnableByDefault();
            DependsOn<UnicastBus>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var outboundTransports = context.Settings.Get<OutboundTransports>().Transports;
            context.Container.ConfigureComponent(c =>
            {
                var dispatchers = new List<Tuple<IDispatchMessages, TransportDefinition>>();
                IDispatchMessages defaultDispatcher = null;

                foreach (var transport in outboundTransports)
                {
                    var sendConfigContext = transport.Configure(context.Settings);
                    var d = sendConfigContext.DispatcherFactory();
                    dispatchers.Add(Tuple.Create(d, transport.Definition));
                    if (transport.IsDefault)
                    {
                        defaultDispatcher = d;
                    }
                }
                return new TransportDispatcher(defaultDispatcher, dispatchers);
            }, DependencyLifecycle.SingleInstance);
        }
    }
}