namespace NServiceBus.Transports
{
    using System;
    using Utils.Reflection;

    class EnableSelectedTransport : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            var configType = config.Settings.GetOrDefault<Type>("Transport");

            configType.Construct<IConfigureTransport>().Configure(config);
        }
    }
}