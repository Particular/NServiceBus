using System;
using NServiceBus.Config;
using NServiceBus.Hosting.Profiles;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;
using Common.Logging;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();

            Configure.Instance.Configurer.ConfigureComponent<Faults.InMemory.FaultManager>(ComponentCallModelEnum.Singleton);

            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                if (e.ExceptionObject.GetType() ==
                    typeof(AccessViolationException))
                    logger.Fatal(
                        "NServiceBus has detected an error in the operation of SQLite. SQLite is the database used to store sagas from NServiceBus when running under the 'Integration' profile. This error usually occurs only under load. If you wish to use sagas under load, it is recommended to run NServiceBus under the 'Production' profile. This can be done by passing the value 'NServiceBus.Production' on the command line to the NServiceBus.Host.exe process. For more information see http://www.NServiceBus.com/Profiles.aspx .");
            };

            if (Config is AsA_Publisher)
            {
                if (Configure.GetConfigSection<MsmqSubscriptionStorageConfig>() == null)
                    Configure.Instance.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(
                        ComponentCallModelEnum.Singleton)
                        .ConfigureProperty(s => s.Queue, Program.EndpointId + "_subscriptions");
                else
                    Configure.Instance.MsmqSubscriptionStorage();
            }
        }

        public IConfigureThisEndpoint Config { get; set; }
        private static ILog logger = LogManager.GetLogger("System.Data.SQLite");
    }
}