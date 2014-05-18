namespace NServiceBus.Hosting.Profiles
{
    using System;
    using System.Collections.Generic;
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
        public ProfileManager(List<Assembly> assembliesToScan, IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles)
        {
            this.assembliesToScan = assembliesToScan;
            this.specifier = specifier;

            var existingProfiles = assembliesToScan.AllTypesAssignableTo<IProfile>();
            var profilesFromArguments = args
                .Select(arg => existingProfiles.SingleOrDefault(profileType => profileType.FullName.ToLower() == arg.ToLower()))
                .Where(t => t != null)
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
                throw new Exception(message);
            }

// ReSharper disable HeapView.SlowDelegateCreation
            return configs.Select(type => (T)Activator.CreateInstance(type));
// ReSharper restore HeapView.SlowDelegateCreation
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
        public void ActivateProfileHandlers(Configure config)
        {
            var instantiableHandlers = assembliesToScan
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IHandleProfile).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();

            var handlersByProfile = activeProfiles
                .Select(p => new
                             {
                                 Profile = p,
                                 HandlerTypes = instantiableHandlers
                                     .Where(handlerType =>
                                            {
                                                var handledProfile = handlerType.GetGenericallyContainedType(typeof(IHandleProfile<>), typeof(IProfile));
                                                return handledProfile != null && handledProfile.IsAssignableFrom(p);
                                            })
                                     .ToList()
                             })
                .ToList();

            var executedHandlers = new List<Type>();

            foreach (var profileWithHandlerTypes in handlersByProfile)
            {
                Logger.Info("Activating profile: " + profileWithHandlerTypes.Profile.AssemblyQualifiedName);

                foreach (var handlerType in profileWithHandlerTypes.HandlerTypes)
                {
                    if (executedHandlers.Contains(handlerType))
                    {
                        Logger.Debug("Profile handler was already activated by a preceding profile: " + handlerType.AssemblyQualifiedName);
                        continue;
                    }
                    var profileHandler = (IHandleProfile)Activator.CreateInstance(handlerType);
                    var wantsActiveProfiles = profileHandler as IWantTheListOfActiveProfiles;
                    if (wantsActiveProfiles != null)
                    {
                        wantsActiveProfiles.ActiveProfiles = activeProfiles;
                    }

                    var wantsTheConfig = profileHandler as IWantTheEndpointConfig;
                    if (wantsTheConfig != null)
                    {
                        wantsTheConfig.Config = specifier;
                    }

                    Logger.Debug("Activating profile handler: " + handlerType.AssemblyQualifiedName);
                    profileHandler.ProfileActivated(config);

                    executedHandlers.Add(handlerType);
                }
            }
        }


        static ILog Logger = LogManager.GetLogger(typeof(ProfileManager));
    }
}