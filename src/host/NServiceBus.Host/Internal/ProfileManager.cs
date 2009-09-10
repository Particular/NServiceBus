using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Host.Profiles;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Scans and loads profile handlers from the given assemblies
    /// </summary>
    public class ProfileManager
    {
        private readonly IEnumerable<Assembly> assembliesToScan;
        private readonly IConfigureThisEndpoint specifier;

        /// <summary>
        /// Initializes the manager with the assemblies to scan and the endpoint configuration to use
        /// </summary>
        /// <param name="assembliesToScan"></param>
        /// <param name="specifier"></param>
        public ProfileManager(IEnumerable<Assembly> assembliesToScan, IConfigureThisEndpoint specifier)
        {
            this.assembliesToScan = assembliesToScan;
            this.specifier = specifier;
        }

        /// <summary>
        /// Loads the profilehandlers that handles the given profile. Defaults to the Production profile 
        /// if no match is found
        /// </summary>
        /// <param name="profileArgs"></param>
        /// <returns></returns>
        public IEnumerable<IHandleProfileConfiguration> GetProfileConfigurationHandlersFor(string[] profileArgs)
        {
            var profiles = GetProfilesFrom(assembliesToScan);

            var handlers = new List<Type>();
            var configurerers = new List<Type>();

            foreach (var assembly in assembliesToScan)
                foreach (var type in assembly.GetTypes())
                {
                    if (null != type.GetGenericallyContainedType(typeof (IHandleProfile<>), typeof (IProfile)))
                        handlers.Add(type);
                    if (null != type.GetGenericallyContainedType(typeof(IHandleProfileConfiguration<>), typeof(IProfile)))
                        configurerers.Add(type);
                }

            var activeProfiles = profiles.Where(t => profileArgs.Any(pa => t.Name.ToLower() == pa));

           
            if(activeProfiles.Count() == 0)
                activeProfiles = DefaultProfile;

            var activeHandlers = handlers.Where(t => activeProfiles.Any(p => typeof(IHandleProfile<>).MakeGenericType(p).IsAssignableFrom(t)));
            var activeConfigurers = configurerers.Where(t => activeProfiles.Any(p => typeof(IHandleProfileConfiguration<>).MakeGenericType(p).IsAssignableFrom(t)));

            var profileConfigurers = new List<IHandleProfileConfiguration>();
            foreach(var c in activeConfigurers)
                profileConfigurers.Add(Activator.CreateInstance(c) as IHandleProfileConfiguration);

            profileConfigurers.ForEach(pc => pc.Init(specifier));

            var profileHandlers = new List<IHandleProfile>();
            foreach (var h in activeHandlers)
                profileHandlers.Add(Activator.CreateInstance(h) as IHandleProfile);

            profileHandlers.ForEach(hp => hp.ProfileActivated());

            return profileConfigurers;
        }


        private static IEnumerable<Type> GetProfilesFrom(IEnumerable<Assembly> assembliesToScan)
        {
            IEnumerable<Type> profiles = new List<Type>();

            foreach (var assembly in assembliesToScan)
                profiles = profiles.Union(assembly.GetTypes().Where(t => typeof(IProfile).IsAssignableFrom(t) && t != typeof(IProfile)));

            return profiles;
        }

        private static readonly IEnumerable<Type> DefaultProfile = new[] { typeof(Production) };

    }
}