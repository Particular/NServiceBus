using NServiceBus;
using NServiceBus.ObjectBuilder;

namespace Timeout.MessageHandlers
{
    /// <summary>
    /// Configures the timeout host.
    /// </summary>
    public class Endpoint : IConfigureThisEndpoint, AsA_Worker, IWantCustomInitialization
    {
        void IWantCustomInitialization.Init()
        {
            var configure = NServiceBus.Configure.With().DefaultBuilder();

            //string nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            //string serialization = ConfigurationManager.AppSettings["Serialization"];

            //switch (serialization)
            //{
            //    case "xml":
            //        configure.XmlSerializer(nameSpace);
            //        break;
            //    case "binary":
            //        configure.BinarySerializer();
            //        break;
            //    default:
            //        throw new ConfigurationErrorsException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
            //}
            configure.JsonSerializer();

            configure.Configurer.ConfigureComponent<TimeoutManager>(DependencyLifecycle.SingleInstance);
            configure.Configurer.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(tp => tp.ConnectionString, "UseDevelopmentStorage=true");
        }
    }

}
