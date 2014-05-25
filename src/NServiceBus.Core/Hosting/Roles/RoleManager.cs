namespace NServiceBus.Hosting.Roles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Helpers;
    using Logging;
    using Unicast.Config;
    using Utils.Reflection;

    /// <summary>
    /// Handles the different roles that are registered
    /// </summary>
    public class RoleManager
    {
        readonly Configure config;
        readonly IDictionary<Type, Type> availableRoles;
        static ILog Logger = LogManager.GetLogger<RoleManager>();

        /// <summary>
        /// Creates the manager with the list of assemblies to scan for roles
        /// </summary>
        public RoleManager(IEnumerable<Assembly> assembliesToScan, Configure config)
        {
            this.config = config;
            availableRoles = assembliesToScan.AllTypes()
                .Select(t => new { Role = t.GetGenericallyContainedType(typeof(IConfigureRole<>), typeof(IRole)), Configurer = t })
                .Where(x => x.Role != null)
                .ToDictionary(key => key.Role, value => value.Configurer);
        }

        /// <summary>
        /// Checks if the specifier contains a given role and uses it to configure the UnicastBus appropriately.
        /// </summary>
        public void ConfigureBusForEndpoint(IConfigureThisEndpoint specifier)
        {
            ConfigUnicastBus unicastBusConfig = null;

            var roleFound = false;
            foreach (var role in availableRoles)
            {
                var roleType = role.Key;
                bool handlesRole;


                if (roleType.IsGenericType)
                {
                    handlesRole =
                        specifier.GetType()
                                 .GetInterfaces()
                                 .Any(
                                     x =>
                                     x.IsGenericType &&
                                     x.GetGenericTypeDefinition() == roleType.GetGenericTypeDefinition());
                }
                else
                {
                    handlesRole = roleType.IsInstanceOfType(specifier);
                }

                if (!handlesRole)
                    continue;
                roleFound = true;

                //apply role
                var roleConfigurer = (IConfigureRole)Activator.CreateInstance(role.Value);

                var configUnicastBus = roleConfigurer.ConfigureRole(specifier, config);

                if (configUnicastBus != null)
                {
                    if (unicastBusConfig != null)
                        throw new InvalidOperationException("Only one role can configure the UnicastBus");

                    unicastBusConfig = configUnicastBus;
                }

                Logger.Info("Role " + roleType + " configured");
                foreach (var markerProfile in GetMarkerRoles(specifier.GetType(), roleType))
                    Logger.Info("Role " + markerProfile + " is marked.");
            }
            if (!roleFound)
            {
                throw new Exception("Did you forget to specify the endpoint role? Please make sure you specify the endpoint role as either 'AsA_Client','AsA_Host','AsA_Publisher', 'AsA_Server' or some other custom 'IRole'.");
            }
        }

        private IEnumerable<string> GetMarkerRoles(Type configuredEndpoint, Type roleType)
        {
            return (from markerProfile in configuredEndpoint.GetInterfaces()
                    where markerProfile != roleType
                    where (markerProfile != typeof(IRole)) && (markerProfile.GetInterface(typeof(IRole).ToString()) != null)
                    select markerProfile.ToString()).ToList();
        }
    }


}