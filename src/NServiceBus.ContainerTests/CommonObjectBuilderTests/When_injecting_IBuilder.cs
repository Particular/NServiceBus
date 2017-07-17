namespace NServiceBus.ContainerTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class When_injecting_IBuilder
    {
        [SetUp]
        public void Setup()
        {
            // reset static instance counter
            UowDependency.InstanceCounter = 0;
        }

        [Test]
        public void Should_inject_current_scope_when_injecting_builder_to_resolve_uow_dependencies()
        {
            using (var builder = new CommonObjectBuilder(TestContainerBuilder.ConstructBuilder()))
            {
                // Current core implementation to resolve IBuilder
                builder.ConfigureComponent(() => (IBuilder)builder, DependencyLifecycle.SingleInstance);

                builder.ConfigureComponent(typeof(ClassResolvingItsDependencies), DependencyLifecycle.InstancePerCall);
                builder.ConfigureComponent(typeof(UowDependency), DependencyLifecycle.InstancePerUnitOfWork);

                var c1 = (ClassResolvingItsDependencies)builder.Build(typeof(ClassResolvingItsDependencies));

                ClassResolvingItsDependencies c2;
                ClassResolvingItsDependencies c3;
                using (var childBuilder = builder.CreateChildBuilder())
                {
                    c2 = (ClassResolvingItsDependencies)childBuilder.Build(typeof(ClassResolvingItsDependencies));
                    c3 = (ClassResolvingItsDependencies)childBuilder.Build(typeof(ClassResolvingItsDependencies));
                }

                Assert.AreNotSame(c1, c2);
                Assert.AreNotSame(c2, c3);

                Assert.AreSame(c2.dependency, c3.dependency);

                Assert.AreEqual(2, UowDependency.InstanceCounter);
                Assert.AreNotSame(c1.dependency, c2.dependency);
            }
        }

        [Test]
        public async Task Should_inject_current_scope_when_injecting_builder_to_resolve_uow_dependencies_asynchronously()
        {
            var builder = await Task.Run(() => new CommonObjectBuilder(TestContainerBuilder.ConstructBuilder()));

            // Current core implementation to resolve IBuilder
            builder.ConfigureComponent(() => (IBuilder)builder, DependencyLifecycle.SingleInstance);

            builder.ConfigureComponent(typeof(ClassResolvingItsDependencies), DependencyLifecycle.InstancePerCall);
            builder.ConfigureComponent(typeof(UowDependency), DependencyLifecycle.InstancePerUnitOfWork);

            ClassResolvingItsDependencies c1 = null;

            await Task.Run(() =>
            {
                c1 = (ClassResolvingItsDependencies)builder.Build(typeof(ClassResolvingItsDependencies));
            });


            ClassResolvingItsDependencies c2 = null;
            ClassResolvingItsDependencies c3 = null;
            await Task.Run(() =>
            {
                using (var childBuilder = builder.CreateChildBuilder())
                {
                    c2 = (ClassResolvingItsDependencies)childBuilder.Build(typeof(ClassResolvingItsDependencies));
                    c3 = (ClassResolvingItsDependencies)childBuilder.Build(typeof(ClassResolvingItsDependencies));
                }
            });

            Assert.AreNotSame(c1, c2);
            Assert.AreNotSame(c2, c3);

            Assert.AreSame(c2.dependency, c3.dependency);

            Assert.AreEqual(2, UowDependency.InstanceCounter);
            Assert.AreNotSame(c1.dependency, c2.dependency);
        }

        class ClassResolvingItsDependencies
        {
            public ClassResolvingItsDependencies(IBuilder builder)
            {
                dependency = builder.Build<UowDependency>();
            }

            public readonly UowDependency dependency;
        }

        class UowDependency
        {
            public UowDependency()
            {
                Interlocked.Increment(ref InstanceCounter);
            }

            public static int InstanceCounter;
        }
    }
}