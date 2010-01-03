using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Logging;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Unicast.Config;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Hosting.Roles
{
    /// <summary>
    /// Handles the different roles that are registered
    /// </summary>
    public class RoleManager
    {
        private readonly IDictionary<Type, Type> availableRoles;
        private static readonly ILog logger = LogManager.GetLogger(typeof (RoleManager));

        /// <summary>
        /// Creates the manager with the list of assemblies to scan for roles
        /// </summary>
        /// <param name="assembliesToScan"></param>
        public RoleManager(IEnumerable<Assembly> assembliesToScan)
        {
            availableRoles = assembliesToScan.AllTypes()
                .Select(t => new {Role = t.GetGenericallyContainedType(typeof (IConfigureRole<>), typeof (IRole)),Configurer=t})
                .Where(x=>x.Role != null)
                .ToDictionary(key => key.Role, value => value.Configurer);;
    
        }

        /// <summary>
        /// Checks if the specifier contains a given role and uses it to configure the UnicastBus appropriately.
        /// </summary>
        /// <param name="specifier"></param>
        public void ConfigureBusForEndpoint(IConfigureThisEndpoint specifier)
        {
            ConfigUnicastBus config = null;

            foreach (var role in availableRoles)
            {
                var roleType = role.Key;
                if (roleType.IsAssignableFrom(specifier.GetType()))
                {
                    if (config != null)
                        throw new InvalidOperationException("Endpoints can only have one role");

                    //apply role
                    var roleConfigurer = Activator.CreateInstance(role.Value) as IConfigureRole;


                    config = roleConfigurer.ConfigureRole(specifier);

                    logger.Info("Role " + roleType + " configured");
                }
            }

            if (specifier is ISpecifyMessageHandlerOrdering)
            {
                if (config == null)
                    throw new ConfigurationException("You must implement a role in order to use ISpecifyMessageHandlerOrdering. If you are doing your own bus configuration, specify the order in .UnicastBus().LoadMessageHandlers(order);");

                (specifier as ISpecifyMessageHandlerOrdering).SpecifyOrder(new Order(config));
            }
            else
                if (config != null)
                    config.LoadMessageHandlers();
        }
    }

   
}