using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Host.Profiles;

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
        /// <param name="profile"></param>
        /// <returns></returns>
        public IEnumerable<IHandleProfileConfiguration> GetProfileConfigurationHandlersFor(string profile)
        {
            var profiles = GetProfilesFrom(assembliesToScan);

            var handlers = GetProfileHandlersFrom(assembliesToScan);

            var activeProfiles = profiles.Where(t => profile.Contains(t.Name.ToLowerInvariant()));

           
            if(activeProfiles.Count() == 0)
                activeProfiles = DefaultProfile;

            var activeHandlers = handlers.Where(t =>
                                                activeProfiles.Any(p => typeof(IHandleProfile<>).MakeGenericType(p).IsAssignableFrom(t)));

            var profileHandlers = new List<IHandleProfile>();
            foreach (var h in activeHandlers)
                profileHandlers.Add(Activator.CreateInstance(h) as IHandleProfile);


            profileHandlers.ForEach(hp => hp.Init(specifier));

            return profileHandlers.Where(hp => hp is IHandleProfileConfiguration)
                .Select(hp => hp as IHandleProfileConfiguration);
        }


        private static IEnumerable<Type> GetProfilesFrom(IEnumerable<Assembly> assembliesToScan)
        {
            IEnumerable<Type> profiles = new List<Type>();

            foreach (var assembly in assembliesToScan)
                profiles = profiles.Union(assembly.GetTypes().Where(t => typeof(IProfile).IsAssignableFrom(t) && t != typeof(IProfile)));

            return profiles;
        }

        private static IEnumerable<Type> GetProfileHandlersFrom(IEnumerable<Assembly> assembliesToScan)
        {
            foreach (var assembly in assembliesToScan)
                foreach (var type in assembly.GetTypes())
                    foreach (var i in type.GetInterfaces())
                    {
                        var args = i.GetGenericArguments();
                        if (args.Length == 1)
                            if (typeof(IProfile).IsAssignableFrom(args[0]))
                                if (typeof(IHandleProfile<>).MakeGenericType(args) == i)
                                {
                                    yield return type;
                                    break;
                                }
                    }
        }

        private static readonly IEnumerable<Type> DefaultProfile = new[] { typeof(Production) };

    }
}