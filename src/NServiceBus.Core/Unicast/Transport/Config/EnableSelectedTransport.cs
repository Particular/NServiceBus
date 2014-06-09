namespace NServiceBus.Transports
{
    using System;
    using Utils.Reflection;

    class EnableSelectedTransport : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            var configType = config.Settings.GetOrDefault<Type>("TransportConfigurer");

            configType.Construct<IConfigureTransport>().Configure(config);
        }
    }
}