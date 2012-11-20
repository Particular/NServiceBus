using System.Linq;
using NUnit.Framework;
using NServiceBus.Hosting.Helpers;

namespace NServiceBus.Hosting.Tests.AssemblyListExtensions
{
    [TestFixture]
    public class AllTypesAssignableToTests
    {
        [Test]
        public void ShouldContainAllImplementations()
        {
            var types = AssemblyPathHelper.GetAllAssemblies()
                .AllTypesAssignableTo<IProfile>()
                .ToList();
            Assert.IsTrue(types.Any(x=>x.Name == "ClassChild"));
            Assert.IsTrue(types.Any(x => x.Name == "IInterfaceChild"));
            Assert.IsTrue(types.Any(x => x.Name == "Abstract"));
            Assert.IsTrue(types.Any(x => x.Name == "Concrete"));
        }

        [Test]
        public void ShouldNotContainSelf()
        {
            var types = AssemblyPathHelper.GetAllAssemblies()
                .AllTypesAssignableTo<IProfile>()
                .ToList();
            Assert.IsFalse(types.Any(x => x.Name == "IProfile"));
        }

        [Test]
        public void ShouldNotContainNonImpl()
        {
            var types = AssemblyPathHelper.GetAllAssemblies()
                .AllTypesAssignableTo<IProfile>()
                .ToList();
            Assert.IsFalse(types.Any(x => x.Name == "ClassNotImplementing"));
        }

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
    }
}