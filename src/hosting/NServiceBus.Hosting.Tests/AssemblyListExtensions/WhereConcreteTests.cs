using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NServiceBus.Hosting.Helpers;

namespace NServiceBus.Hosting.Tests.AssemblyListExtensions
{
    [TestFixture]
    public class WhereConcreteTests
    {

        [Test]
        public void ValidateIncludedTypes()
        {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .WhereConcrete()
                .ToList();
            Assert.IsTrue(types.Any(x => x.Name == "Class"));
            Assert.IsTrue(types.Any(x => x.Name == "Concrete"));
            Assert.IsFalse(types.Any(x => x.Name == "Abstract"));
            Assert.IsFalse(types.Any(x => x.Name == "IInterfaceChild"));
        }

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
    }
}