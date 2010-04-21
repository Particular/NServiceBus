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
            config.Configurer.ConfigureProperty<ClassWithSetterDependency>(x => x.EnumDependency, SomeEnum.X);
            config.Configurer.ConfigureProperty<ClassWithSetterDependency>(x => x.SimpleDependecy, 1);
            config.Configurer.ConfigureProperty<ClassWithSetterDependency>(x => x.StringDependecy, "Test");


            var component = config.Builder.Build<ClassWithSetterDependency>();

            component.Dependency.ShouldNotBeNull();

            component.EnumDependency.ShouldEqual(SomeEnum.X);
            component.SimpleDependecy.ShouldEqual(1);
            component.StringDependecy.ShouldEqual("Test");
        }


        [Test]
        public void Ordering_of_component_registrations_should_not_matter()
        {
            config.Configurer.ConfigureComponent<ClassWithSetterDependency>(ComponentCallModelEnum.Singleton);


            config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singleton);

            var component = config.Builder.Build<ClassWithSetterDependency>();

            component.Dependency.ShouldNotBeNull();
        }

        [Test]
        public void Should_register_singleton_instance_using_type()
        {
            var instance = new ClassThatImplementsDependency();

            this.config.Configurer.RegisterSingleton(typeof(ClassThatImplementsDependency), instance);

            var component = this.config.Builder.Build<ClassThatImplementsDependency>();

            component.GetHashCode().ShouldEqual(instance.GetHashCode());
        }

        [Test]
        public void Should_register_singleton_instance_using_generics()
        {
            var instance = new ClassThatImplementsDependency();

            this.config.Configurer.RegisterSingleton<ClassThatImplementsDependency>(instance);

            var component = this.config.Builder.Build<ClassThatImplementsDependency>();

            component.GetHashCode().ShouldEqual(instance.GetHashCode());
        }
    }


    


}
