using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                var stopwatch = Stopwatch.StartNew();
            var allAssemblies = Directory.EnumerateFiles(directoryName, "*.dll")
                .Select(Assembly.LoadFrom)
                .ToList();
            stopwatch.Stop();
            Debug.WriteLine(string.Format("Load Assemblies: {0}ms", stopwatch.ElapsedMilliseconds));
            return allAssemblies;
        }

    }
}