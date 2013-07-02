namespace NServiceBus.Hosting.Profiles
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;
    using Helpers;
    using Logging;
    using Utils.Reflection;

    /// <summary>
    /// Scans and loads profile handlers from the given assemblies
    /// </summary>
    public class ProfileManager
    {
        IEnumerable<Assembly> assembliesToScan;
        internal List<Type> activeProfiles;
        IConfigureThisEndpoint specifier;

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

            var profilesFromArguments = assembliesToScan
                .AllTypesAssignableTo<IProfile>()
                .Where(t => args.Any(a => t.FullName.ToLower() == a.ToLower()))
                .ToList();

            if (profilesFromArguments.Count == 0)
            {
                activeProfiles = new List<Type>(defaultProfiles);
            }
            else
            {
                var allProfiles = new List<Type>(profilesFromArguments);
                allProfiles.AddRange(profilesFromArguments.SelectMany(_ => _.GetInterfaces().Where(IsProfile)));
                activeProfiles = allProfiles.Distinct().ToList();
            }
        }

        static bool IsProfile(Type t)
        {
            return typeof(IProfile).IsAssignableFrom(t) && t != typeof(IProfile);
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
            {
                foreach (var type in a.GetTypes())
                {
                    if (typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        options.Add(type);
                    }
                }
            }

            var configs = activeProfiles
                .SelectMany(_ => FindConfigurerForProfile(openGenericType, _, options))
                .ToList();

            if (configs.Count == 0)
            {
                var message = string.Format("Could not find a class which implements '{0}'. If you've specified your own profile, try implementing '{0}ForProfile<T>' for your profile.", typeof(T).Name);
                throw new ConfigurationErrorsException(message);
            }

            return configs.Select(_ => (T)Activator.CreateInstance(_));
        }


        IEnumerable<Type> FindConfigurerForProfile(Type openGenericType, Type profile, List<Type> options)
        {
            if (profile == typeof (object) || profile == null)
            {
                yield break;
            }

            foreach (var option in options)
            {
                var p = option.GetGenericallyContainedType(openGenericType, typeof(IProfile));
                if (p == profile)
                {
                    yield return option;
                }
            }

            foreach (var option in FindConfigurerForProfile(openGenericType, profile.BaseType, options))
            {
                yield return option;
            }
             
        }

        /// <summary>
        /// Activates the profile handlers that handle the previously identified active profiles. 
        /// </summary>
        /// <returns></returns>
        public void ActivateProfileHandlers()
        {
            foreach (var p in activeProfiles)
            {
                Logger.Info("Going to activate profile: " + p.AssemblyQualifiedName);
            }

            var activeHandlers = new List<Type>();

            foreach (var assembly in assembliesToScan)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var p = type.GetGenericallyContainedType(typeof (IHandleProfile<>), typeof (IProfile));
                    if (p != null)
                    {
                        activeHandlers.AddRange(from ap in activeProfiles where (type.IsClass && !type.IsAbstract && p.IsAssignableFrom(ap) && !activeHandlers.Contains(type)) select type);
                    }
                }
            }

            var profileHandlers = new List<IHandleProfile>();
            foreach (var h in activeHandlers)
            {
                var profileHandler = Activator.CreateInstance(h) as IHandleProfile;
                var handler = profileHandler as IWantTheListOfActiveProfiles;
                if (handler != null)
                {
                    handler.ActiveProfiles = activeProfiles;
                }

                profileHandlers.Add(profileHandler);
                Logger.Debug("Activating profile handler: " + h.AssemblyQualifiedName);
            }

            profileHandlers.Where(ph => ph is IWantTheEndpointConfig)
                .ToList()
                .ForEach(
                ph => (ph as IWantTheEndpointConfig).Config = specifier);

            profileHandlers.ForEach(hp => hp.ProfileActivated());
        }


        static ILog Logger = LogManager.GetLogger(typeof(ProfileManager));
    }
}