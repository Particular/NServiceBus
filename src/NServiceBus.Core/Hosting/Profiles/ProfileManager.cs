using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
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
        internal readonly List<Type> activeProfiles;
        private readonly IConfigureThisEndpoint specifier;

        /// <summary>
        /// Initializes the manager with the assemblies to scan and the endpoint configuration to use
        /// </summary>
        /// <param name="assembliesToScan"></param>
        /// <param name="specifier"></param>
        /// <param name="args"></param>
        /// <param name="defaultProfiles"></param>
        public ProfileManager(List<Assembly> assembliesToScan, IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles)
        {
            this.assembliesToScan = assembliesToScan;
            this.specifier = specifier;

            var profiles = assembliesToScan
                .AllTypesAssignableTo<IProfile>()
                .Where(t => args.Any(a => t.FullName.ToLower() == a.ToLower()))
                .ToList();

            if (profiles.Count == 0)
                profiles = defaultProfiles;

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
        public IEnumerable<IConfigureLogging> GetLoggingConfigurer()
        {
            return GetImplementor<IConfigureLogging>(typeof(IConfigureLoggingForProfile<>));
        }

        internal IEnumerable<T> GetImplementor<T>(Type openGenericType) where T : class
        {
            var options = new List<Type>();
            foreach (var a in assembliesToScan)
                foreach (var t in a.GetTypes())
                    if (typeof(T).IsAssignableFrom(t) && !t.IsInterface)
                        options.Add(t);

            return FindConfigurer<T>(options, list =>
                                              activeProfiles.Select(ap => FindConfigurerForProfile(openGenericType, ap, list.ToList())).Where(t => t != null)
                );
        }

        private IEnumerable<T> FindConfigurer<T>(List<Type> options, Func<IEnumerable<Type>, IEnumerable<Type>> filter) where T : class
        {
            var myOptions = new List<Type>(filter(options));

            if (myOptions.Count == 0)
            {
                var message = string.Format("Could not find a class which implements '{0}'. If you've specified your own profile, try implementing '{0}ForProfile<T>' for your profile.", typeof(T).Name);
                throw new ConfigurationErrorsException(message);
            }

            foreach (var option in myOptions)
            {
                yield return (T)Activator.CreateInstance(option);
            }
        }

        private Type FindConfigurerForProfile(Type openGenericType, Type profile, List<Type> options)
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
                        activeHandlers.AddRange(from ap in activeProfiles where (type.IsClass && !type.IsAbstract && p.IsAssignableFrom(ap) && !activeHandlers.Contains(type)) select type);
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


        private static ILog Logger = LogManager.GetLogger(typeof(ProfileManager));
    }
}