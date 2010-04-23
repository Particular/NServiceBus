using System;
using System.Configuration;
using NServiceBus;
using NServiceBus.Sagas.Impl;

namespace Timeout.MessageHandlers
{
    /// <summary>
    /// Configures the timeout host.
    /// </summary>
    public class Endpoint : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization, ISpecifyMessageHandlerOrdering
    {
        void IWantCustomInitialization.Init()
        {
            var configure = NServiceBus.Configure.With().DefaultBuilder();

            string nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            string serialization = ConfigurationManager.AppSettings["Serialization"];

            switch (serialization)
            {
                case "xml":
                    configure.XmlSerializer(nameSpace);
                    break;
                case "binary":
                    configure.BinarySerializer();
                    break;
                default:
                    throw new ConfigurationErrorsException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
            }
        }

        void ISpecifyMessageHandlerOrdering.SpecifyOrder(Order order)
        {
            order.Specify(First<TimeoutMessageHandler>.Then<SagaMessageHandler>());
        }
    }

    /// <summary>
    /// Configures performance behavior of the timeout manager
    /// </summary>
    public class PerformanceConfig : IWantCustomInitialization
    {
        void IWantCustomInitialization.Init()
        {
            string maxSagaIdsToStore = ConfigurationManager.AppSettings["MaxSagasIdsToStore"];
            string millisToSleepBetweenMessages = ConfigurationManager.AppSettings["MillisToSleepBetweenMessages"];
            
            int sagas = 1000;
            if (!string.IsNullOrEmpty(maxSagaIdsToStore))
                int.TryParse(maxSagaIdsToStore, out sagas);

            NServiceBus.Configure.Instance.Configurer.ConfigureProperty<TimeoutMessageHandler>(h => h.MaxSagaIdsToStore, sagas);

            int millis = 10;
            if (!string.IsNullOrEmpty(millisToSleepBetweenMessages))
                int.TryParse(millisToSleepBetweenMessages, out millis);

            NServiceBus.Configure.Instance.Configurer.ConfigureProperty<TimeoutMessageHandler>(h => h.MillisToSleepBetweenMessages, millis);
        }
    }
}
