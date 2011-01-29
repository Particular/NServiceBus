using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Installation
{
    public static class Install
    {
        public static Installer<T> ForInstallationOn<T>(this Configure config) where T : IEnvironment
        {
            if (config.Configurer == null)
                throw new InvalidOperationException("No container found. Please call '.DefaultBuilder()' after 'Configure.With()' before calling this method (or provide an alternative container).");

            return new Installer<T>();
        }

        public class Installer<T> where T : IEnvironment
        {
            public void Install()
            {
                var listOfCompatibleTypes = new List<Type>();
                var listOfInstallers = new List<Type>();

                var envType = typeof (T);
                while(envType != typeof(object))
                {
                    listOfCompatibleTypes.Add(typeof (INeedToInstallSomething<>).MakeGenericType(envType));
                    envType = envType.BaseType;
                }

                foreach(var t in Configure.TypesToScan)
                    foreach (var i in listOfCompatibleTypes)
                        if (i.IsAssignableFrom(t))
                            listOfInstallers.Add(t);
                    
                listOfInstallers.ForEach(t => ((INeedToInstallSomething)Configure.Instance.Builder.Build(t)).Install());
            }
        }
    }
}
