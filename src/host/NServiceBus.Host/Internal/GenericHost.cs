using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NServiceBus.Host.Internal.ProfileHandlers;
using NServiceBus.Host.Profiles;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Implementation which hooks into TopShelf's Start/Stop lifecycle.
    /// </summary>
    public class GenericHost : MarshalByRefObject
    {
        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            var busConfiguration  = new ConfigurationBuilder(spec, profileConfigurationHandlers).Build();
          
            Action startupAction = null;

            if (spec is ISpecify.StartupAction)
                startupAction = (spec as ISpecify.StartupAction).StartupAction;

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (!(spec is IDontWant.TheBusStartedAutomatically))
                busConfiguration.CreateBus().Start(startupAction);

            if (messageEndpoint == null)
                return;

            //give it its own thread so that logging continues to work.
            Action onstart = () => messageEndpoint.OnStart();
            onstart.BeginInvoke(null, null);
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            if (messageEndpoint != null)
                messageEndpoint.OnStop();
        }

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="args"></param>
        public GenericHost(Type endpointType, string[] args)
        {
            var profiles = GetProfilesFrom(GetType().Assembly).Union(
                GetProfilesFrom(endpointType.Assembly));

            var handlers = GetProfileHandlersFrom(GetType().Assembly).Union(
                GetProfileHandlersFrom(endpointType.Assembly));

            var a = string.Join(" ", args).ToLowerInvariant();

            var activeProfiles = profiles.Where(t => a.Contains(t.Name.ToLowerInvariant())) ??
                new[] {typeof(Production)};

            var activeHandlers = handlers.Where(t =>
                activeProfiles.Any(p => typeof(IHandleProfile<>).MakeGenericType(p).IsAssignableFrom(t)));

            var profileHandlers = new List<IHandleProfile>();
            foreach (var h in activeHandlers)
                profileHandlers.Add(Activator.CreateInstance(h) as IHandleProfile);

            spec = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            profileHandlers.ForEach(hp => hp.Init(spec));

            profileConfigurationHandlers = profileHandlers.Where(hp => hp is IHandleProfileConfiguration)
                .Select(hp => hp as IHandleProfileConfiguration);
        }

        private static IEnumerable<Type> GetProfilesFrom(Assembly a)
        {
            return a.GetTypes().Where(t => typeof (IProfile).IsAssignableFrom(t) && t != typeof(IProfile));
        }

        private static IEnumerable<Type> GetProfileHandlersFrom(Assembly a)
        {
            foreach(Type t in a.GetTypes())
                foreach (var i in t.GetInterfaces())
                {
                    var args = i.GetGenericArguments();
                    if (args.Length == 1)
                        if (typeof(IProfile).IsAssignableFrom(args[0]))
                            if (typeof(IHandleProfile<>).MakeGenericType(args) == i)
                            {
                                yield return t;
                                break;
                            }
                }
        }

        private IMessageEndpoint messageEndpoint;
        private readonly IEnumerable<IHandleProfileConfiguration> profileConfigurationHandlers;
        private readonly IConfigureThisEndpoint spec;
    }
}