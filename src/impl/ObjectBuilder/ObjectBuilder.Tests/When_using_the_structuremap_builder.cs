using System.Collections.Generic;
using NServiceBus;
using NServiceBus.ObjectBuilder;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace ObjectBuilder.Tests
{
    [TestFixture]
    public class When_using_the_structuremap_builder
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
             config = Configure.With()
              .StructureMapBuilder();
        }

        [Test]
        public void Trying_to_build_a_non_configured_instance_should_not_throw()
        {
            Assert.DoesNotThrow(() => config.Builder.Build<INonConfiguredInterface>());
        }

         [Test]
        public void Should_satisfy_setter_dependencies()
        {

            config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<ClassWithSetterDependency>(ComponentCallModelEnum.Singleton);


             config.Builder.Build<ClassWithSetterDependency>().Dependency.ShouldNotBeNull();
        }


         [Test]
         public void Ordering_of_component_registrations_should_not_matter()
         {
             config.Configurer.ConfigureComponent<ClassWithSetterDependency>(ComponentCallModelEnum.Singleton);

             
             config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singleton);


             config.Builder.Build<ClassWithSetterDependency>().Dependency.ShouldNotBeNull();

         }


    }

    
    public class ClassWithSetterDependency
    {
        public ISomeDependency Dependency { get; set; }
        public IList<string> SystemDependency { get; set; }
    }

    public interface ISomeDependency{}

    public class ClassThatImplementsDependency:ISomeDependency{}

    public interface INonConfiguredInterface { }


}
