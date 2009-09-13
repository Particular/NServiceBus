using System;
using Common.Logging;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Handles the client/server bus configuration.
    /// </summary>
    public static class RoleManager
    {
        /// <summary>
        /// Checks if the specifier is a client or server and sets up the MsmqTransport and UnicastBus approproiately.
        /// </summary>
        /// <param name="specifier"></param>
        public static void ConfigureBusForEndpoint(IConfigureThisEndpoint specifier)
        {
            if (specifier is AsA_Client && specifier is AsA_Server)
                throw new InvalidOperationException("Cannot specify endpoint both as a client and as a server.");

            ConfigUnicastBus config = null;

            if (specifier is AsA_Client)
                config = ConfigureClientRole();

            if (specifier is AsA_Server)
                config = ConfigureServerRole();

            if (specifier is ISpecifyMessageHandlerOrdering)
            {
                if (config == null)
                    throw new ConfigurationException("You must implement either AsA_Client or AsA_Server to use ISpecifyMessageHandlerOrdering. If you are doing your own bus configuration, specify the order in .UnicastBus().LoadMessageHandlers(order);");

                (specifier as ISpecifyMessageHandlerOrdering).SpecifyOrder(new Order(config));
            }
            else
                if (config != null)
                    config.LoadMessageHandlers();
        }

        private static ConfigUnicastBus ConfigureClientRole()
        {
            return Configure.Instance
                .MsmqTransport()
                .IsTransactional(false)
                .PurgeOnStartup(true)
                .UnicastBus()
                .ImpersonateSender(false);
        }

        private static ConfigUnicastBus ConfigureServerRole()
        {
            return Configure.Instance
                .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
                .UnicastBus()
                .ImpersonateSender(true);
        }
    }
}
