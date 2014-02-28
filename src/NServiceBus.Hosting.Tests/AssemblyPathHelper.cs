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
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            var directoryName = Path.GetDirectoryName(path);
            var files = Directory.EnumerateFiles(directoryName, "*.dll").ToList();

            return files
                .Select(Assembly.LoadFrom)
                .ToList();
        }
    }
}