namespace NServiceBus.Core.Tests.Config
{
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_loading_types
    {
      
        [Test]
        public void Should_always_include_the_core_nservicebus_types()
        {
             var builder = new ConfigurationBuilder();

            builder.AssembliesToScan(Assembly.GetExecutingAssembly());

            Assert.True(builder.BuildConfiguration().Settings.GetAvailableTypes().Any(a => a.Assembly.GetName().Name.Equals("NServiceBus.Core")));
        }
    }

    public class TestClass
    {
        
    }
}