namespace NServiceBus.ContainerTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_resolving_instances_in_factory_method
    {
        [SetUp]
        public void Setup()
        {
            // reset static instance counter
            InstancePerCallWithUowDependency.InstanceCounter = 0;
            UowScopeDependency.InstanceCounter = 0;
        }

        [Test]
        public void Should_use_current_scope_when_resolving_uow_dependencies_with_builder_factory()
        {
            using (var builder = new CommonObjectBuilder(TestContainerBuilder.ConstructBuilder()))
            {
                builder.ConfigureComponent(typeof(UowScopeDependency), DependencyLifecycle.InstancePerUnitOfWork);

                // current CommonObjectBuilder implementation for factory method passing an IBuilder instance:
                builder.ConfigureComponent(() => new InstancePerCallWithUowDependency((UowScopeDependency)builder.Build(typeof(UowScopeDependency))), DependencyLifecycle.InstancePerCall);

                var rootScopeInstance1 = (InstancePerCallWithUowDependency)builder.Build(typeof(InstancePerCallWithUowDependency));
                var rootScopeInstance2 = (InstancePerCallWithUowDependency)builder.Build(typeof(InstancePerCallWithUowDependency));

                InstancePerCallWithUowDependency childBuilder1Instance1;
                InstancePerCallWithUowDependency childBuilder1Instance2;
                using (var childBuilder1 = builder.CreateChildBuilder())
                {
                    childBuilder1Instance1 = (InstancePerCallWithUowDependency)childBuilder1.Build(typeof(InstancePerCallWithUowDependency));
                    childBuilder1Instance2 = (InstancePerCallWithUowDependency)childBuilder1.Build(typeof(InstancePerCallWithUowDependency));
                }

                InstancePerCallWithUowDependency childBuilder2Instance1;
                InstancePerCallWithUowDependency childBuilder2Instance2;
                using (var childBuilder2 = builder.CreateChildBuilder())
                {
                    childBuilder2Instance1 = (InstancePerCallWithUowDependency)childBuilder2.Build(typeof(InstancePerCallWithUowDependency));
                    childBuilder2Instance2 = (InstancePerCallWithUowDependency)childBuilder2.Build(typeof(InstancePerCallWithUowDependency));
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
        }

        // This test ensures that the internal implementation doesn't rely on CallContext/AsyncLocal which is dropped by the way NServiceBus is initialized & started.
        [Test]
        public async Task Should_use_current_scope_when_resolving_uow_dependencies_with_builder_factory_asynchronously()
        {
            var builder = await Task.Run(() => new CommonObjectBuilder(TestContainerBuilder.ConstructBuilder()));

            builder.ConfigureComponent(typeof(UowScopeDependency), DependencyLifecycle.InstancePerUnitOfWork);

            // current CommonObjectBuilder implementation for factory method passing an IBuilder instance:
            builder.ConfigureComponent(() => new InstancePerCallWithUowDependency((UowScopeDependency)builder.Build(typeof(UowScopeDependency))), DependencyLifecycle.InstancePerCall);

            InstancePerCallWithUowDependency rootScopeInstance1 = null;
            InstancePerCallWithUowDependency rootScopeInstance2 = null;
            await Task.Run(() =>
            {
                rootScopeInstance1 = (InstancePerCallWithUowDependency)builder.Build(typeof(InstancePerCallWithUowDependency));
                rootScopeInstance2 = (InstancePerCallWithUowDependency)builder.Build(typeof(InstancePerCallWithUowDependency));
            });
            

            InstancePerCallWithUowDependency childBuilder1Instance1 = null;
            InstancePerCallWithUowDependency childBuilder1Instance2 = null;
            await Task.Run(() =>
            {
                using (var childBuilder1 = builder.CreateChildBuilder())
                {
                    childBuilder1Instance1 = (InstancePerCallWithUowDependency)childBuilder1.Build(typeof(InstancePerCallWithUowDependency));
                    childBuilder1Instance2 = (InstancePerCallWithUowDependency)childBuilder1.Build(typeof(InstancePerCallWithUowDependency));
                }
            });

            InstancePerCallWithUowDependency childBuilder2Instance1 = null;
            InstancePerCallWithUowDependency childBuilder2Instance2 = null;
            await Task.Run(() =>
            {
                using (var childBuilder2 = builder.CreateChildBuilder())
                {
                    childBuilder2Instance1 = (InstancePerCallWithUowDependency)childBuilder2.Build(typeof(InstancePerCallWithUowDependency));
                    childBuilder2Instance2 = (InstancePerCallWithUowDependency)childBuilder2.Build(typeof(InstancePerCallWithUowDependency));
                }
            });

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

            builder.Dispose();
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
    }
}