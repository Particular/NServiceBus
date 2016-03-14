namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Core.Tests.Config;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_directory_with_nested_directories
    {          
        [Test]
        public void Should_not_scan_nested_directories_by_default()
        {
            var endpointConfiguration = new EndpointConfiguration("myendpoint");
            endpointConfiguration.ExcludeTypes(typeof(When_using_initialization_with_non_default_ctor.FeatureWithInitialization));
            endpointConfiguration.Build();

            var scanedTypes = endpointConfiguration.Settings.Get<IList<Type>>("TypesToScan");
            var foundTypeFromNestedAssembly = scanedTypes.Any(x => x.Name == "NestedClass");
            Assert.False(foundTypeFromNestedAssembly, "Was expected not to scan nested assemblies, but nested assembly was scanned.");
        }


        [Test]
        public void Should_scan_nested_directories_if_requested()
        {
            var endpointConfiguration = new EndpointConfiguration("myendpoint");
            endpointConfiguration.ScanAssembliesInNestedDirectories();
            endpointConfiguration.ExcludeTypes(typeof(When_using_initialization_with_non_default_ctor.FeatureWithInitialization));
            endpointConfiguration.Build();

            var scanedTypes = endpointConfiguration.Settings.Get<IList<Type>>("TypesToScan");
            var foundTypeFromNestedAssembly = scanedTypes.Any(x => x.Name == "NestedClass");
            Assert.True(foundTypeFromNestedAssembly, "Was expected to scan nested assemblies, but nested assembly were not scanned.");
        }
    }
}