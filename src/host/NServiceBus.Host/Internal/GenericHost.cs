using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using NServiceBus.Host.Internal.ProfileHandlers;

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
            Trace.WriteLine("Starting host for " + endpointType.FullName);

            var configurationSpecifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            var busConfiguration  = new ConfigurationBuilder(configurationSpecifier, profileHandlers).Build();
          
            Action startupAction = null;

            if (configurationSpecifier is ISpecify.StartupAction)
                startupAction = (configurationSpecifier as ISpecify.StartupAction).StartupAction;

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (!(configurationSpecifier is IDontWant.TheBusStartedAutomatically))
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
            this.endpointType = endpointType;

            var profiles = GetProfilesFrom(GetType().Assembly).Union(
                GetProfilesFrom(endpointType.Assembly));

            var handlers = GetProfileHandlersFrom(GetType().Assembly).Union(
                GetProfileHandlersFrom(endpointType.Assembly));

            var a = string.Join(" ", args).ToLowerInvariant();

            var activeProfiles = profiles.Where(t => a.Contains(t.Name.ToLowerInvariant()));

            var activeHandlers = handlers.Where(t =>
                activeProfiles.Any(p => typeof(IHandleProfile<>).MakeGenericType(p).IsAssignableFrom(t)));

            profileHandlers = activeHandlers.Select(t => Activator.CreateInstance(t) as IHandleProfile) ??
                              new[] {new ProductionProfileHandler()};
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
                            if (typeof (IHandleProfile<>).MakeGenericType(args) == i)
                            {
                                yield return t;
                                break;
                            }
                }
        }

        private readonly Type endpointType;
        private IMessageEndpoint messageEndpoint;
        private readonly IEnumerable<IHandleProfile> profileHandlers;
    }
}