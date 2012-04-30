using System.CodeDom;

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
        public void Should_exclude_the_raven_client_types()
        {
            CollectionAssert.AreEquivalent(
                new Type[0],
                loadedTypes.Where(a => a.Namespace.StartsWith("Raven.Client")).ToArray());
        }
    }
}
namespace Raven.Client
{
    public class TestClass
    {}
}