using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using NServiceBus.Installation;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to the Configure class.
    /// </summary>
    public static class Install
    {
        /// <summary>
        /// Test Method for Installation
        /// </summary>
        public static void Test()
        {
            Configure.With().ForInstallationOn<IEnvironment>();
        }
        /// <summary>
        /// Indicates which environment is going to be installed, specifying that resources 
        /// to be created will be provided permissions for the currently logged on user.
        /// </summary>
        /// <typeparam name="T">The environment type.</typeparam>
        /// <param name="config">Extension method object.</param>
        /// <returns>An Installer object whose Install method should be invoked.</returns>
        public static Installer<T> ForInstallationOn<T>(this Configure config) where T : IEnvironment
        {
            // todo When code is .Net 4.0 Only, remove this method in favor of optional parameters
            return ForInstallationOn<T>(config, null);
        }

        /// <summary>
        /// Indicates which environment is going to be installed, specifying that resources 
        /// to be created will be provided permissions for the user represented by the userToken
        /// (where not the currently logged on user) or the currently logged on user.
        /// </summary>
        /// <typeparam name="T">The environment type.</typeparam>
        /// <param name="config">Extension method object.</param>
        /// <param name="userToken">A token that will be used to create a <see cref="WindowsIdentity"/>.</param>
        /// <returns>An Installer object whose Install method should be invoked.</returns>
        public static Installer<T> ForInstallationOn<T>(this Configure config, IntPtr? userToken) where T : IEnvironment
        {
            if (config.Configurer == null)
                throw new InvalidOperationException("No container found. Please call '.DefaultBuilder()' after 'Configure.With()' before calling this method (or provide an alternative container).");

            WindowsIdentity identity;
           
            // Passing a token results in a duplicate identity exception in some cases, you can't compare tokens so this could
            // still happen but at least the explicit WindowsIdentity.GetCurrent().Token is avoided now.
            if (userToken.HasValue)
                identity = new WindowsIdentity(userToken.Value);
            else
                identity = WindowsIdentity.GetCurrent();

            return new Installer<T>(identity);
        }
    }
    
    /// <summary>
    /// Resolves objects who implement INeedToInstall and invokes them for the given environment.
    /// Assumes that implementors have already been registered in the container.
    /// </summary>
    /// <typeparam name="T">The environment for which the installers should be invoked.</typeparam>
    public class Installer<T> where T : IEnvironment
    {
        static Installer()
        {
            RunOtherInstallers = true;
        }
        /// <summary>
        /// Initializes a new instance of the Installer
        /// </summary>
        /// <param name="identity">WindowsIdentity</param>
        public Installer(WindowsIdentity identity)
        {
            winIdentity = identity;
        }

        private WindowsIdentity winIdentity;

        /// <summary>
        /// Gets or sets RunInfrastructureInstallers 
        /// </summary>
        public static bool RunInfrastructureInstallers { get; set; }
        /// <summary>
        /// Gets or sets RunOtherInstallers 
        /// </summary>
        public static bool RunOtherInstallers { private get; set; }

        private static bool installedInfrastructureInstallers = false;
        private static bool installedOthersInstallers = false;
        private static bool queuesCreated = false;

        /// <summary>
        /// Invokes installers for the given environment
        /// </summary>
        public void Install()
        {
            Configure.Instance.Initialize();

            if(RunInfrastructureInstallers)
                InstallInfrastructureInstallers();
            
            if(RunOtherInstallers)            
                InstallOtherInstallers();

            CreateQueues();
        }

        private void CreateQueues()
        {
            if (queuesCreated)
                return;

            GetInstallers<T>(typeof(IWantQueuesCreated<>))
                .ForEach(t => ((IWantQueuesCreated)Configure.Instance.Builder.Build(t)).Create(winIdentity));

            queuesCreated = true;
        }

        /// <summary>
        /// Invokes only Infrastructure installers for the given environment.
        /// </summary>
        public void InstallInfrastructureInstallers()
        {
            if (installedInfrastructureInstallers)
                return;

            GetInstallers<T>(typeof(INeedToInstallInfrastructure<>))
                .ForEach(t => ((INeedToInstallInfrastructure)Activator.CreateInstance(t)).Install(winIdentity));
            
            installedInfrastructureInstallers = true;
        }

        /// <summary>
        /// Invokes only 'Something' - other than infrastructure,  installers for the given environment.
        /// </summary>
        private void InstallOtherInstallers()
        {
            if (installedOthersInstallers)
                return;

            GetInstallers<T>(typeof(INeedToInstallSomething<>))
                .ForEach(t => ((INeedToInstallSomething)Activator.CreateInstance(t)).Install(winIdentity));
            
            installedOthersInstallers = true;
        }


        private static List<Type> GetInstallers<TEnvtype>(Type openGenericInstallType) where TEnvtype : IEnvironment
        {
            var listOfCompatibleTypes = new List<Type>();

            var envType = typeof(TEnvtype);
            while (envType != typeof(object))
            {
                listOfCompatibleTypes.Add(openGenericInstallType.MakeGenericType(envType));
                envType = envType.BaseType;
            }

            return (from t in Configure.TypesToScan 
                    from i in listOfCompatibleTypes 
                    where i.IsAssignableFrom(t) 
                    select t).ToList();
        }
    }
}
