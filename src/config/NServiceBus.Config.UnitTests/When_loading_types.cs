namespace NServiceBus.Config.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_loading_types
    {
        List<Type> loadedTypes;

        [SetUp]
        public void SetUp()
        {
            Configure.With(Assembly.GetExecutingAssembly());
            loadedTypes = Configure.TypesToScan.ToList();
        }

        [Test]
        public void Should_exclude_the_raven_types()
        {
            Assert.False(
                loadedTypes.Any(a => a.Namespace != null && a.Namespace.StartsWith("Raven")));
        }

        [Test]
        public void Should_always_include_the_core_nservicebus_types()
        {
            Assert.True(
                loadedTypes.Any(a => a.Assembly.GetName().Name.Equals("NServiceBus.Config")));
        }
    }
}

namespace Raven
{
    public class TestClass
    { }
}