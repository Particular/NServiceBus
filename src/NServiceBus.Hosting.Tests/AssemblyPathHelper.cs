namespace NServiceBus.Hosting.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class AssemblyPathHelper
    {
        public static List<Assembly> GetAllAssemblies()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string directoryName = Path.GetDirectoryName(path);
            List<string> files = Directory.EnumerateFiles(directoryName, "*.dll").ToList();

            List<Assembly> allAssemblies = files
                .Select(Assembly.LoadFrom)
                .ToList();

            return allAssemblies;
        }
    }
}