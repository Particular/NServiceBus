using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NServiceBus.Hosting.Helpers;

namespace NServiceBus.Hosting.Tests
{
    [TestFixture]
    public class ReflectionPerfTests
    {
        //Do not run SubSetAssemblies and AllAssemblies in the same test session. we want to test cold starts
        [Test]
        public void SubSetAssemblies()
        {
            var assemblies = Directory.EnumerateFiles(AssemblyDirectory, "*.dll")
                .Select(Assembly.LoadFrom)
                .ToList();

            var stopwatch = Stopwatch.StartNew();
            var types = assemblies
                .AllTypesAssignableTo<IWantCustomInitialization>()
                .WhereConcrete()
                .Where(t => !typeof(IConfigureThisEndpoint).IsAssignableFrom(t))
                .ToList();
            stopwatch.Stop();
            Debug.WriteLine(stopwatch.ElapsedMilliseconds + "ms");
        }

        [Test]
        public void AllAssemblies()
        {
            var assemblies = Directory.EnumerateFiles(AssemblyDirectory, "*.dll")
                .Select(Assembly.LoadFrom)
                .ToList();

            var stopwatch = Stopwatch.StartNew();
            var types = new List<Type>();
            foreach(var a in assemblies)
            {
                foreach (var t in a.GetTypes())
                {
                    if (typeof (IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !typeof (IConfigureThisEndpoint).IsAssignableFrom(t))
                    {
                        types.Add(t);
                    }
                }
            }
            stopwatch.Stop();
            Debug.WriteLine(stopwatch.ElapsedMilliseconds + "ms");
        }
        static public string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}