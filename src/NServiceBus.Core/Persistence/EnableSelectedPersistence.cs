namespace NServiceBus.Persistence
{
    using System;
    using System.Linq;

    class EnableSelectedPersistence:IWantToRunBeforeConfigurationIsFinalized,IWantToRunBeforeConfiguration
    {
        public void Run(Configure config)
        {
            var definitionType = config.Settings.Get<Type>("Persistence");

            var type =
             config.TypesToScan.SingleOrDefault(
                 t => typeof(IConfigurePersistence<>).MakeGenericType(definitionType).IsAssignableFrom(t));

            if (type == null)
                throw new InvalidOperationException(
                    "We couldn't find a IConfigurePersistence implementation for your selected persistence: " +
                    definitionType.Name);

            ((IConfigurePersistence)Activator.CreateInstance(type)).Enable(config);
        }

        public void Init(Configure configure)
        {
            configure.Settings.SetDefault("Persistence", typeof(InMemory));
        }
    }
}