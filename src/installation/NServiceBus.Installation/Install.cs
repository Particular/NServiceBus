using System;
using System.Collections.Generic;

namespace NServiceBus.Installation
{
    /// <summary>
    /// Contains extension methods to the Configure class.
    /// </summary>
    public static class Install
    {
        /// <summary>
        /// Indicates which environment is going to be installed.
        /// </summary>
        /// <typeparam name="T">The environment type.</typeparam>
        /// <param name="config">Extension method object.</param>
        /// <returns>An Installer object whose Install method should be invoked.</returns>
        public static Installer<T> ForInstallationOn<T>(this Configure config) where T : IEnvironment
        {
            if (config.Configurer == null)
                throw new InvalidOperationException("No container found. Please call '.DefaultBuilder()' after 'Configure.With()' before calling this method (or provide an alternative container).");

            return new Installer<T>();
        }

        /// <summary>
        /// Resolves objects who implement INeedToInstall and invokes them for the given environment.
        /// </summary>
        /// <typeparam name="T">The environment for which the installers should be invoked.</typeparam>
        public class Installer<T> where T : IEnvironment
        {
            /// <summary>
            /// Invokes all installers for the given environment.
            /// </summary>
            public void Install()
            {
                GetInstallers<T>(typeof(INeedToInstallInfrastructure<>))
                    .ForEach(t => ((INeedToInstallInfrastructure)Configure.Instance.Builder.Build(t)).Install());

                GetInstallers<T>(typeof(INeedToInstallSomething<>))
                    .ForEach(t => ((INeedToInstallSomething)Configure.Instance.Builder.Build(t)).Install());
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
}
