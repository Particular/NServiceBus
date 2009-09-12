using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Logging;
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
        private readonly IEnumerable<Type> activeProfiles;

        /// <summary>
        /// Initializes the manager with the assemblies to scan and the endpoint configuration to use
        /// </summary>
        /// <param name="assembliesToScan"></param>
        /// <param name="specifier"></param>
        /// <param name="profileArgs"></param>
        public ProfileManager(IEnumerable<Assembly> assembliesToScan, IConfigureThisEndpoint specifier, string[] profileArgs)
        {
            this.assembliesToScan = assembliesToScan;
            this.specifier = specifier;

            if (specifier is IDontWantProfiles)
            {
                Logger.Debug("Configured not to use profiles.");
                return;
            }

            var profiles = GetProfilesFrom(assembliesToScan);

            activeProfiles = profiles.Where(t => profileArgs.Any(pa => t.FullName.ToLower() == pa.ToLower()));

            if (activeProfiles.Count() == 0)
                activeProfiles = DefaultProfile;
        }

        /// <summary>
        /// Returns an object to configure logging based on the specification and profiles passed in.
        /// </summary>
        /// <returns></returns>
        public IConfigureLogging GetLoggingConfigurer()
        {
            IConfigureLogging result;

            var options = new List<Type>();
            foreach(var a in assembliesToScan)
                foreach(var t in a.GetTypes())
                    if (typeof(IConfigureLogging).IsAssignableFrom(t) && !t.IsInterface)
                        options.Add(t);

            if (specifier is IDontWantProfiles)
            {
                result = FindLoggingConfigurer(options, list => list.Where(item =>
                        null == item.GetGenericallyContainedType(typeof(IConfigureLoggingForProfile<>), typeof(IProfile)))
                    );
            }
            else
            {
                result = FindLoggingConfigurer(options, list => 
                    activeProfiles.Select(ap => FindLoggingConfigurerForProfile(ap, list)).Where(t => t != null)
                    );
            }

            Logger.Debug("Going to use " + result.GetType().FullName + " to configure logging.");
            return result;
        }

        private IConfigureLogging FindLoggingConfigurer(IEnumerable<Type> options, Func<IEnumerable<Type>, IEnumerable<Type>> filter)
        {
            var myOptions = new List<Type>(filter(options));

            if (myOptions.Count == 0)
                throw new ConfigurationException("Could not find a class which implements IConfigureLogging.");
            if (myOptions.Count > 1)
                throw new ConfigurationException("More than one class which implements IConfigureLogging was found: " + string.Join(" ", options.Select(t => t.Name).ToArray()));

            return Activator.CreateInstance(myOptions[0]) as IConfigureLogging;
        }

        private Type FindLoggingConfigurerForProfile(Type profile, IEnumerable<Type> options)
        {
            if (profile == typeof(object)) return null;

            foreach(var o in options)
            {
                var p = o.GetGenericallyContainedType(typeof (IConfigureLoggingForProfile<>), typeof (IProfile));
                if (p == profile)
                    return o;
            }

            //couldn't find exact match - try again recursively going up the type hierarchy
            return FindLoggingConfigurerForProfile(profile.BaseType, options);
        }

        /// <summary>
        /// Loads the profilehandlers that handles the given profile. Defaults to the Production profile 
        /// if no match is found
        /// </summary>
        /// <param name="profileArgs"></param>
        /// <returns></returns>
        public IEnumerable<IHandleProfileConfiguration> GetProfileConfigurationHandlersFor(string[] profileArgs)
        {
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
                profiles = profiles.Union(assembly.GetTypes().Where(t => typeof(IProfile).IsAssignableFrom(t) && !t.IsInterface));

            return profiles;
        }

        private static readonly IEnumerable<Type> DefaultProfile = new[] { typeof(Production) };

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Host");
    }
}