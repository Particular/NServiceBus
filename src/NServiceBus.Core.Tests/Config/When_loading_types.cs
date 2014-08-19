namespace NServiceBus.Core.Tests.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_loading_types
    {
        private List<Type> loadedTypes;

        [SetUp]
        public void SetUp()
        {
            var builder = new ConfigurationBuilder();

            builder.AssembliesToScan(Assembly.GetExecutingAssembly());

            var configure = Configure.With(builder);
            loadedTypes = configure.TypesToScan.ToList();
        }

        [Test]
        public void Should_always_include_the_core_nservicebus_types()
        {
            Assert.True(
                loadedTypes.Any(a => a.Assembly.GetName().Name.Equals("NServiceBus.Core")));
        }
    }

    public class TestClass
    {
        
    }
}