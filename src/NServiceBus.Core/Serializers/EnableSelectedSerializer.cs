namespace NServiceBus.Features.Categories
{
    using System;

    class EnableSelectedSerializer : IWantToRunBeforeConfiguration,IWantToRunBeforeConfigurationIsFinalized
    {   
        public void Run(Configure config)
        {
            config.Features.Enable(config.Settings.GetOrDefault<Type>("SelectedSerializer"));
        }

        public void Init(Configure configure)
        {
            configure.Settings.SetDefault("SelectedSerializer",typeof(XmlSerialization));
        }
    }
}