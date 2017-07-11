namespace NServiceBus.Core.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class CommonObjectBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            UowDependency.InstanceCounter = 0;
            InstancePerCallWithUowDependency.InstanceCounter = 0;
        }

        [Test]
        public void When_using_injected_builder_to_resolve_uow_dependencies()
        {
            var builder = new CommonObjectBuilder(new LightInjectObjectBuilder());

            IBuilder iBuilder = builder;

            builder.ConfigureComponent(b => iBuilder, DependencyLifecycle.SingleInstance);

            builder.ConfigureComponent<ClassResolvingItsDependencies>(DependencyLifecycle.InstancePerCall);
            builder.ConfigureComponent<UowDependency>(DependencyLifecycle.InstancePerUnitOfWork);

            ClassResolvingItsDependencies c2;
            ClassResolvingItsDependencies c3;

            var c1 = builder.Build<ClassResolvingItsDependencies>();

            using (var childBuilder = builder.CreateChildBuilder())
            {
                c2 = childBuilder.Build<ClassResolvingItsDependencies>();
                c3 = childBuilder.Build<ClassResolvingItsDependencies>();
            }

            Assert.AreNotSame(c1, c2);
            Assert.AreNotSame(c2, c3);

            Assert.AreSame(c2.dependency, c3.dependency);

            Assert.AreEqual(2, UowDependency.InstanceCounter);
            Assert.AreNotSame(c1.dependency, c2.dependency);
        }

        [Test]
        public async Task When_using_injected_builder_on_another_thread_to_resolve_uow_dependencies()
        {
            var builder = new CommonObjectBuilder(new LightInjectObjectBuilder());

            IBuilder iBuilder = builder;

            builder.ConfigureComponent(b => iBuilder, DependencyLifecycle.SingleInstance);

            builder.ConfigureComponent<ClassResolvingItsDependencies>(DependencyLifecycle.InstancePerCall);
            builder.ConfigureComponent<UowDependency>(DependencyLifecycle.InstancePerUnitOfWork);

            ClassResolvingItsDependencies c2 = null;
            ClassResolvingItsDependencies c3 = null;

            var c1 = builder.Build<ClassResolvingItsDependencies>();

            await Task.Run(async () =>
            {
                using (var childBuilder = builder.CreateChildBuilder())
                {
                    c2 = childBuilder.Build<ClassResolvingItsDependencies>();
                    await Task.Yield();
                    c3 = childBuilder.Build<ClassResolvingItsDependencies>();
                }
            });

            Assert.AreNotSame(c1, c2);
            Assert.AreNotSame(c2, c3);

            Assert.AreSame(c2.dependency, c3.dependency);

            Assert.AreEqual(2, UowDependency.InstanceCounter);
            Assert.AreNotSame(c1.dependency, c2.dependency);
        }

        [Test]
        public void When_resolving_uow_dependencies_configured_with_builder_factory()
        {
            var builder = new CommonObjectBuilder(new LightInjectObjectBuilder());

            builder.ConfigureComponent<UowScopeDependency>(DependencyLifecycle.InstancePerUnitOfWork);
            builder.ConfigureComponent(b => new InstancePerCallWithUowDependency(b.Build<UowScopeDependency>()), DependencyLifecycle.InstancePerCall);

            var rootScopeInstance1 = builder.Build<InstancePerCallWithUowDependency>();
            var rootScopeInstance2 = builder.Build<InstancePerCallWithUowDependency>();

            InstancePerCallWithUowDependency childBuilder1Instance1;
            InstancePerCallWithUowDependency childBuilder1Instance2;
            using (var childBuilder1 = builder.CreateChildBuilder())
            {
                childBuilder1Instance1 = childBuilder1.Build<InstancePerCallWithUowDependency>();
                childBuilder1Instance2 = childBuilder1.Build<InstancePerCallWithUowDependency>();
            }

            InstancePerCallWithUowDependency childBuilder2Instance1;
            InstancePerCallWithUowDependency childBuilder2Instance2;
            using (var childBuilder2 = builder.CreateChildBuilder())
            {
                childBuilder2Instance1 = childBuilder2.Build<InstancePerCallWithUowDependency>();
                childBuilder2Instance2 = childBuilder2.Build<InstancePerCallWithUowDependency>();
            }

            // check instance per call instances
            Assert.AreNotSame(rootScopeInstance1, rootScopeInstance2);
            Assert.AreNotSame(childBuilder1Instance1, childBuilder1Instance2);
            Assert.AreNotSame(childBuilder2Instance1, childBuilder2Instance2);

            Assert.AreNotSame(rootScopeInstance1, childBuilder1Instance1);
            Assert.AreNotSame(rootScopeInstance1, childBuilder2Instance1);

            Assert.AreNotSame(childBuilder1Instance2, childBuilder2Instance2);

            // check dependency references
            Assert.AreSame(rootScopeInstance1.dependency, rootScopeInstance2.dependency);
            Assert.AreSame(childBuilder1Instance1.dependency, childBuilder1Instance2.dependency);
            Assert.AreSame(childBuilder2Instance1.dependency, childBuilder2Instance2.dependency);

            Assert.AreNotSame(rootScopeInstance1.dependency, childBuilder2Instance1.dependency);
            Assert.AreNotSame(rootScopeInstance1.dependency, childBuilder2Instance1.dependency);
            Assert.AreNotSame(childBuilder1Instance1.dependency, childBuilder2Instance1.dependency);

            // check instantiation count
            Assert.AreEqual(6, InstancePerCallWithUowDependency.InstanceCounter);
            Assert.AreEqual(3, UowScopeDependency.InstanceCounter);
        }

        class InstancePerCallWithUowDependency
        {
            public InstancePerCallWithUowDependency(UowScopeDependency dependency)
            {
                this.dependency = dependency;
                Interlocked.Increment(ref InstanceCounter);
            }

            // ReSharper disable once NotAccessedField.Local
            public readonly UowScopeDependency dependency;

            public static int InstanceCounter;
        }

        class UowScopeDependency
        {
            public UowScopeDependency()
            {
                Interlocked.Increment(ref InstanceCounter);
            }

            public static int InstanceCounter;
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