namespace NServiceBus.Hosting.Tests.AssemblyListExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class AllTypesAssignableToTests
    {
        public interface IInterfaceChild : IProfile
        {
        }

        public class ClassChild : IInterfaceChild
        {
        }

        public class ClassNotImplementing
        {
        }

        public abstract class Abstract : IProfile
        {
        }

        public class Concrete : Abstract
        {
        }

        [Test]
        public void ShouldContainAllImplementations()
        {
            List<Type> types = AssemblyPathHelper.GetAllAssemblies()
                                                 .AllTypesAssignableTo<IProfile>()
                                                 .ToList();
            Assert.IsTrue(types.Any(x => x.Name == "ClassChild"));
            Assert.IsTrue(types.Any(x => x.Name == "IInterfaceChild"));
            Assert.IsTrue(types.Any(x => x.Name == "Abstract"));
            Assert.IsTrue(types.Any(x => x.Name == "Concrete"));
        }

        [Test]
        public void ShouldNotContainNonImpl()
        {
            List<Type> types = AssemblyPathHelper.GetAllAssemblies()
                                                 .AllTypesAssignableTo<IProfile>()
                                                 .ToList();
            Assert.IsFalse(types.Any(x => x.Name == "ClassNotImplementing"));
        }

        [Test]
        public void ShouldNotContainSelf()
        {
            List<Type> types = AssemblyPathHelper.GetAllAssemblies()
                                                 .AllTypesAssignableTo<IProfile>()
                                                 .ToList();
            Assert.IsFalse(types.Any(x => x.Name == "IProfile"));
        }
    }
}