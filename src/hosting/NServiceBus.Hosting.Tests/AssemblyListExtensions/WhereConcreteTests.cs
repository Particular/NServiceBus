namespace NServiceBus.Hosting.Tests.AssemblyListExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class WhereConcreteTests
    {
        public interface IInterfaceChild
        {
        }

        public class Class
        {
        }

        public abstract class Abstract
        {
        }

        public class Concrete : Abstract
        {
        }

        [Test]
        public void ValidateIncludedTypes()
        {
            List<Type> types = Assembly.GetExecutingAssembly()
                                       .GetTypes()
                                       .WhereConcrete()
                                       .ToList();
            Assert.IsTrue(types.Any(x => x.Name == "Class"));
            Assert.IsTrue(types.Any(x => x.Name == "Concrete"));
            Assert.IsFalse(types.Any(x => x.Name == "Abstract"));
            Assert.IsFalse(types.Any(x => x.Name == "IInterfaceChild"));
        }
    }
}