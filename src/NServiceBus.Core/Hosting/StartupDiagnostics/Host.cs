namespace NServiceBus
{
    using System;
    using System.Reflection;
    using IODirectory = System.IO.Directory;

    static class Host
    {
        public static string GetOutputDirectory()
        {
            Assembly systemWebAssembly = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "System.Web")
                {
                    systemWebAssembly = assembly;
                    break;
                }
            }

            var httpRuntime = systemWebAssembly?.GetType("System.Web.HttpRuntime");
            var appDomainAppId = httpRuntime?.GetProperty("AppDomainAppId", BindingFlags.Public | BindingFlags.Static);
            var result = appDomainAppId?.GetValue(null);

            if (result == null)
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            return DeriveAppDataPath(systemWebAssembly);
        }

        static string DeriveAppDataPath(Assembly systemWebAssembly)
        {
            var appDataPath = TryMapPath(systemWebAssembly);

            if (appDataPath == null)
            {
                throw new Exception(GetMapPathError("Failed since MapPath returned null"));
            }

            if (IODirectory.Exists(appDataPath))
            {
                return appDataPath;
            }

            throw new Exception(GetMapPathError($"Failed since path returned ({appDataPath}) does not exist. Ensure this directory is created and restart the endpoint."));
        }

        static string TryMapPath(Assembly systemWebAssembly)
        {
            try
            {
                var hostingEnvironment = systemWebAssembly?.GetType("System.Web.Hosting.HostingEnvironment");
                var mapPath = hostingEnvironment?.GetMethod("MapPath", BindingFlags.Static | BindingFlags.Public);
                var result = mapPath?.Invoke(null, new[] { "~/App_Data/" }) as string;

                return result;
            }
            catch (Exception exception)
            {
                throw new Exception(GetMapPathError("Failed since MapPath threw an exception"), exception);
            }
        }

        static string GetMapPathError(string reason)
        {
            return $"Detected running in a website and attempted to use HostingEnvironment.MapPath(\"~/App_Data/\") to derive the logging path. {reason}. To avoid using HostingEnvironment.MapPath to derive the logging directory you can instead configure it to a specific path using LogManager.Use<DefaultFactory>().Directory(\"pathToLoggingDirectory\");";
        }
    }
}