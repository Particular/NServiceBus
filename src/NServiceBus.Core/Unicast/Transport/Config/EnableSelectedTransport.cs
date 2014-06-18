namespace NServiceBus.Transports
{
    using System;
    using Utils.Reflection;

    class EnableSelectedTransport : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            //default to MSMQ
            if (!config.Settings.HasSetting<TransportDefinition>())
            {
                config.UseTransport<NServiceBus.Msmq>();
            }

            var configType = config.Settings.GetOrDefault<Type>("TransportConfigurer");

            configType.Construct<IConfigureTransport>().Configure(config);
        }
    }
}