using System.Linq;
using Autofac.Core.Registration;
using NBehave.Spec.NUnit;
using NServiceBus;
using NServiceBus.ObjectBuilder;
using NUnit.Framework;

namespace ObjectBuilder.Tests
{
    [TestFixture]
    public class When_using_the_autofac2_builder
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            this.config = Configure.With().Autofac2Builder();
        }

        [Test]
        public void Trying_to_build_a_non_configured_instance_should_throw()
        {
            Assert.Throws<ComponentNotRegisteredException>(() => this.config.Builder.Build<INonConfiguredInterface>());
        }

        [Test]
        public void Should_satisfy_setter_dependencies()
        {
            this.config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singleton);
            this.config.Configurer.ConfigureComponent<ClassWithSetterDependency>(ComponentCallModelEnum.Singleton);
            this.config.Configurer.ConfigureProperty<ClassWithSetterDependency>(x => x.EnumDependency, SomeEnum.X);
            this.config.Configurer.ConfigureProperty<ClassWithSetterDependency>(x => x.SimpleDependecy, 1);
            this.config.Configurer.ConfigureProperty<ClassWithSetterDependency>(x => x.StringDependecy, "Test");

            var component = this.config.Builder.Build<ClassWithSetterDependency>();

            component.Dependency.ShouldNotBeNull();

            component.EnumDependency.ShouldEqual(SomeEnum.X);
            component.SimpleDependecy.ShouldEqual(1);
            component.StringDependecy.ShouldEqual("Test");
        }


        [Test]
        public void Ordering_of_component_registrations_should_not_matter()
        {
            this.config.Configurer.ConfigureComponent<ClassWithSetterDependency>(ComponentCallModelEnum.Singleton);

            this.config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singleton);

            var component = this.config.Builder.Build<ClassWithSetterDependency>();

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

        [Test]
        public void Should_allow_multiple_registrations_of_the_same_component()
        {
            var instance = new ClassThatImplementsDependency();

            this.config.Configurer.RegisterSingleton<ClassThatImplementsDependency>(instance);

            this.config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singlecall);
            this.config.Configurer.ConfigureComponent<ClassThatImplementsDependency>(ComponentCallModelEnum.Singlecall);

            var components = this.config.Builder.BuildAll<ClassThatImplementsDependency>();

            components.Count().ShouldEqual(1);
        }
    }
}