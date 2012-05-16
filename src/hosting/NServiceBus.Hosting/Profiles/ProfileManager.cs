using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using NServiceBus.Logging;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Hosting.Profiles
{
    /// <summary>
    /// Scans and loads profile handlers from the given assemblies
    /// </summary>
    public class ProfileManager
    {
        private readonly IEnumerable<Assembly> assembliesToScan;
        private readonly IEnumerable<Type> activeProfiles;
        private readonly IConfigureThisEndpoint specifier;

        /// <summary>
        /// Initializes the manager with the assemblies to scan and the endpoint configuration to use
        /// </summary>
        /// <param name="assembliesToScan"></param>
        /// <param name="specifier"></param>
        /// <param name="args"></param>
        /// <param name="defaultProfiles"></param>
        public ProfileManager(IEnumerable<Assembly> assembliesToScan, IConfigureThisEndpoint specifier, string[] args, IEnumerable<Type> defaultProfiles)
        {
            this.assembliesToScan = assembliesToScan;
            this.specifier = specifier;
            var profiles = new List<Type>(GetProfilesFrom(assembliesToScan).Where(t => args.Any(a => t.FullName.ToLower() == a.ToLower())));

            if (profiles.Count() == 0)
                profiles = defaultProfiles.ToList().ConvertAll(p => p);

            var implements = new List<Type>(profiles);
            foreach (var interfaces in profiles.Select(p => p.GetInterfaces()))
            {
                implements.AddRange(interfaces.Where(t => (typeof(IProfile).IsAssignableFrom(t) && t != typeof(IProfile)) && !profiles.Contains(t)));
            }
            activeProfiles = implements;
        }

        /// <summary>
        /// Returns an object to configure logging based on the specification and profiles passed in.
        /// </summary>
        /// <returns></returns>
        public IConfigureLogging GetLoggingConfigurer()
        {
            return GetImplementor<IConfigureLogging>(typeof(IConfigureLoggingForProfile<>));
        }

        private T GetImplementor<T>(Type openGenericType) where T : class
        {
            var options = new List<Type>();
            foreach (var a in assembliesToScan)
                foreach (var t in a.GetTypes())
                    if (typeof(T).IsAssignableFrom(t) && !t.IsInterface)
                        options.Add(t);

            return FindConfigurer<T>(options, list =>
                                              activeProfiles.Select(ap => FindConfigurerForProfile(openGenericType, ap, list)).Where(t => t != null)
                );
        }

        private T FindConfigurer<T>(IEnumerable<Type> options, Func<IEnumerable<Type>, IEnumerable<Type>> filter) where T : class
        {
            var myOptions = new List<Type>(filter(options));

            if (myOptions.Count == 0)
                throw new ConfigurationErrorsException("Could not find a class which implements " + typeof(T).Name + ". If you've specified your own profile, try implementing " + typeof(T).Name + "ForProfile<T> for your profile.");
            if (myOptions.Count > 1)
                throw new ConfigurationErrorsException("Can not have more than one class configured which implements " + typeof(T).Name + ". Implementors found: " + string.Join(" ", options.Select(t => t.Name).ToArray()));

            return Activator.CreateInstance(myOptions[0]) as T;
        }

        private Type FindConfigurerForProfile(Type openGenericType, Type profile, IEnumerable<Type> options)
        {
            if (profile == typeof(object) || profile == null) return null;

            foreach (var o in options)
            {
                var p = o.GetGenericallyContainedType(openGenericType, typeof(IProfile));
                if (p == profile)
                    return o;
            }

            //couldn't find exact match - try again recursively going up the type hierarchy
            return FindConfigurerForProfile(openGenericType, profile.BaseType, options);
        }

        /// <summary>
        /// Activates the profile handlers that handle the previously identified active profiles. 
        /// </summary>
        /// <returns></returns>
        public void ActivateProfileHandlers()
        {
            foreach (var p in activeProfiles)
                Logger.Info("Going to activate profile: " + p.AssemblyQualifiedName);

            var activeHandlers = new List<Type>();

            foreach (var assembly in assembliesToScan)
                foreach (var type in assembly.GetTypes())
                {
                    var p = type.GetGenericallyContainedType(typeof(IHandleProfile<>), typeof(IProfile));
                    if (p != null)
                        activeHandlers.AddRange(from ap in activeProfiles where (p.IsAssignableFrom(ap) && !activeHandlers.Contains(type)) select type);
                }

            var profileHandlers = new List<IHandleProfile>();
            foreach (var h in activeHandlers)
            {
                var profileHandler = Activator.CreateInstance(h) as IHandleProfile;
                if (profileHandler is IWantTheListOfActiveProfiles)
                    ((IWantTheListOfActiveProfiles)profileHandler).ActiveProfiles = activeProfiles;

                profileHandlers.Add(profileHandler);
                Logger.Debug("Activating profile handler: " + h.AssemblyQualifiedName);
            }

            profileHandlers.Where(ph => ph is IWantTheEndpointConfig).ToList().ForEach(
                ph => (ph as IWantTheEndpointConfig).Config = specifier);

            profileHandlers.ForEach(hp => hp.ProfileActivated());
        }

        private static IEnumerable<Type> GetProfilesFrom(IEnumerable<Assembly> assembliesToScan)
        {
            IEnumerable<Type> profiles = new List<Type>();

            foreach (var assembly in assembliesToScan)
                profiles = profiles.Union(assembly.GetTypes().Where(t => typeof(IProfile).IsAssignableFrom(t) && t != typeof(IProfile)));

            return profiles;
        }

        private static ILog Logger = LogManager.GetLogger("NServiceBus.Host");
    }
}