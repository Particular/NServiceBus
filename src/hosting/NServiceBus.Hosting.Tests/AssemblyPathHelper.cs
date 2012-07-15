using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NServiceBus.Hosting.Tests
{
    public class AssemblyPathHelper{
        public static List<Assembly> GetAllAssemblies()
        {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                var directoryName = Path.GetDirectoryName(path);

                return Directory.EnumerateFiles(directoryName, "*.dll")
                    .Select(Assembly.LoadFrom)
                    .ToList();
        }

    }
}