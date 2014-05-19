namespace NServiceBus.Persistence
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using Logging;

    class EnableSelectedPersistence:IWantToRunBeforeConfigurationIsFinalized,IWantToRunBeforeConfiguration
    {
        public void Run(Configure config)
        {
            var definitionType = config.Settings.GetOrDefault<Type>("Persistence");

            if (definitionType == null)
            {
                throw new Exception("No persistence has been selected, please add a call to config.UsePersistence<T>() where T can be any of the supported persistence options");
            }

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
            if (SystemInformation.UserInteractive)
            {
                configure.Settings.SetDefault("Persistence", typeof(InMemory));    
            }
            else
            {
                Logger.Info("Non interactive mode detected, no persistence will be defaulted");
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(EnableSelectedPersistence));
    }
}