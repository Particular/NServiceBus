namespace NServiceBus.Persistence
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using Logging;

    class EnableSelectedPersistence:IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            var definitionType = config.Settings.GetOrDefault<Type>("Persistence");

            if (definitionType == null)
            {
                if (SystemInformation.UserInteractive)
                {
                    const string warningMessage = "No persistence has been selected, NServiceBus will now use InMemory persistence. We recommend that you change the persistence before deploying to production. To do this,  please add a call to config.UsePersistence<T>() where T can be any of the supported persistence options supported. http://docs.particular.net/nservicebus/persistence-in-nservicebus.";
                    Logger.Warn(warningMessage);
                }
                else
                {
                    const string errorMessage = "No persistence has been selected, please add a call to config.UsePersistence<T>() where T can be any of the supported persistence options supported. http://docs.particular.net/nservicebus/persistence-in-nservicebus";
                    throw new Exception(errorMessage);    
                }
                
                definitionType = typeof(InMemory);
            }

            var type = config.TypesToScan.SingleOrDefault(t => typeof(IConfigurePersistence<>).MakeGenericType(definitionType).IsAssignableFrom(t));

            if (type == null)
            {
                throw new InvalidOperationException("We couldn't find a IConfigurePersistence implementation for your selected persistence: " + definitionType.Name);
            }
                
            ((IConfigurePersistence)Activator.CreateInstance(type)).Enable(config);
        }

        static ILog Logger = LogManager.GetLogger<EnableSelectedPersistence>();
    }
}