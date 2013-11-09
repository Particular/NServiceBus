namespace NServiceBus.Licensing
{
    using System;
    using System.Reflection;

    static class NServiceBusVersion
    {
        static NServiceBusVersion()
        {
            
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            MajorAndMinor = new Version(assemblyVersion.Major, assemblyVersion.Minor).ToString(2);
        }

        public static string MajorAndMinor;
    }
}