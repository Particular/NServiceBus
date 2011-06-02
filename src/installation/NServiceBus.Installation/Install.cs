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
            var i = WindowsIdentity.GetCurrent();
            return ForInstallationOn<T>(config, i.Token);
        }

        /// <summary>
        /// Indicates which environment is going to be installed, specifying that resources 
        /// to be created will be provided permissions for the currently logged on user.
        /// </summary>
        /// <typeparam name="T">The environment type.</typeparam>
        /// <param name="config">Extension method object.</param>
        /// <param name="userToken">A token that will be used to create a <see cref="WindowsIdentity"/>.</param>
        /// <returns>An Installer object whose Install method should be invoked.</returns>
        public static Installer<T> ForInstallationOn<T>(this Configure config, IntPtr userToken) where T : IEnvironment
        {
            if (config.Configurer == null)
                throw new InvalidOperationException("No container found. Please call '.DefaultBuilder()' after 'Configure.With()' before calling this method (or provide an alternative container).");

            Configure.TypesToScan.Where(t => typeof(INeedToInstallSomething).IsAssignableFrom(t) && t.IsInterface == false)
                .ToList().ForEach(t => config.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            return new Installer<T>(new WindowsIdentity(userToken));
        }
    }

    /// <summary>
    /// Resolves objects who implement INeedToInstall and invokes them for the given environment.
    /// Assumes that implementors have already been registered in the container.
    /// </summary>
    /// <typeparam name="T">The environment for which the installers should be invoked.</typeparam>
    public class Installer<T> where T : IEnvironment
    {
        public Installer(WindowsIdentity identity)
        {
            winIdentity = identity;
        }

        private WindowsIdentity winIdentity;

        /// <summary>
        /// Invokes all installers for the given environment.
        /// </summary>
        public void Install()
        {
            NServiceBus.Configure.Instance.Initialize();

            GetInstallers<T>(typeof(INeedToInstallInfrastructure<>))
                .ForEach(t => ((INeedToInstallInfrastructure)Configure.Instance.Builder.Build(t)).Install(winIdentity));

            GetInstallers<T>(typeof(INeedToInstallSomething<>))
                .ForEach(t => ((INeedToInstallSomething)Configure.Instance.Builder.Build(t)).Install(winIdentity));
        }

        private static List<Type> GetInstallers<TEnvtype>(Type openGenericInstallType) where TEnvtype : IEnvironment
        {
            var listOfCompatibleTypes = new List<Type>();
            var listOfInstallers = new List<Type>();

            var envType = typeof(TEnvtype);
            while (envType != typeof(object))
            {
                listOfCompatibleTypes.Add(openGenericInstallType.MakeGenericType(envType));
                envType = envType.BaseType;
            }

            foreach (var t in Configure.TypesToScan)
                foreach (var i in listOfCompatibleTypes)
                    if (i.IsAssignableFrom(t))
                        listOfInstallers.Add(t);

            return listOfInstallers;
        }
    }
}
