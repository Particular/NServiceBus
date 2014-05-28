namespace NServiceBus.Features.Categories
{
    using System;

    class EnableSelectedSerializer : IWantToRunBeforeConfiguration,IWantToRunBeforeConfigurationIsFinalized
    {
        public void Init(Configure configure)
        {
            configure.Settings.SetDefault("SelectedSerializer", typeof(XmlSerialization));
        }

        public void Run(Configure config)
        {
            config.Features(f => f.Enable(config.Settings.GetOrDefault<Type>("SelectedSerializer")));
        }

    }
}