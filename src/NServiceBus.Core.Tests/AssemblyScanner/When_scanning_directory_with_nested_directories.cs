namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_directory_with_nested_directories
    {          
        [Test]
        public void Should_not_scan_nested_directories_by_default()
        {
            var busConfiguration = new BusConfiguration();
            busConfiguration.BuildConfiguration();

            var scanedTypes = busConfiguration.Settings.Get<IList<Type>>("TypesToScan");
            var foundTypeFromNestedAssembly = scanedTypes.Any(x => x.Name == "NestedClass");
            Assert.False(foundTypeFromNestedAssembly, "Was expected not to scan nested assemblies, but nested assembly was scanned.");
        }


        [Test]
        public void Should_scan_nested_directories_if_requested()
        {
            var busConfiguration = new BusConfiguration();
            busConfiguration.ScanAssembliesInNestedDirectories();
            busConfiguration.BuildConfiguration();

            var scanedTypes = busConfiguration.Settings.Get<IList<Type>>("TypesToScan");
            var foundTypeFromNestedAssembly = scanedTypes.Any(x => x.Name == "NestedClass");
            Assert.True(foundTypeFromNestedAssembly, "Was expected to scan nested assemblies, but nested assembly were not scanned.");
        }
    }
}