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
            var definitionType = config.Settings.Get<Type>("Persistence");

            if (definitionType == null)
            {
                const string message = "No persistence has been selected, please add a call to config.UsePersistence<T>() where T can be any of the supported persistence options supported. http://docs.particular.net/nservicebus/persistence-in-nservicebus";

                if (SystemInformation.UserInteractive)
                {
                    Logger.Warn(message);    
                }
                else
                {
                    throw new Exception(message);    
                }
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