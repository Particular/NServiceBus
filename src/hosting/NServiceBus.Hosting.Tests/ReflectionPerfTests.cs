using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            var assemblies = AssemblyPathHelper.GetAllAssemblies();
            var stopwatch = Stopwatch.StartNew();
            var types1 = assemblies
                .AllTypesAssignableTo<IWantCustomInitialization>()
                .WhereConcrete()
                .Where(t => !typeof (IConfigureThisEndpoint).IsAssignableFrom(t))
                .ToList();
            stopwatch.Stop();
            Debug.WriteLine("Find implementations 1: "+  stopwatch.ElapsedMilliseconds + "ms");
            stopwatch = Stopwatch.StartNew();
            var types2 = assemblies
                .AllTypesAssignableTo<IWantCustomInitialization>()
                .WhereConcrete()
                .Where(t => !typeof (IConfigureThisEndpoint).IsAssignableFrom(t))
                .ToList();
            stopwatch.Stop();
            Debug.WriteLine("Find implementations 2: "+  stopwatch.ElapsedMilliseconds + "ms");
        }

        [Test]
        public void AllAssemblies()
        {
            var assemblies = AssemblyPathHelper.GetAllAssemblies();
            var stopwatch = Stopwatch.StartNew();
            var types1 = (from a in assemblies from t in a.GetTypes() where typeof (IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !typeof (IConfigureThisEndpoint).IsAssignableFrom(t) select t).ToList();
            stopwatch.Stop();
            Debug.WriteLine("Find implementations 1: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch = Stopwatch.StartNew();
            var types2 = (from a in assemblies from t in a.GetTypes() where typeof (IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !typeof (IConfigureThisEndpoint).IsAssignableFrom(t) select t).ToList();
            stopwatch.Stop();
            Debug.WriteLine("Find implementations 2: " + stopwatch.ElapsedMilliseconds + "ms");

        }
    }
}