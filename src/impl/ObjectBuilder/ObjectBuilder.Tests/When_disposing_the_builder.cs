namespace ObjectBuilder.Tests
{
    using System;
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Spring;
    using NUnit.Framework;

    [TestFixture]
    public class When_disposing_the_builder : BuilderFixture
    {
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            ForAllBuilders(builder =>
                {
                    SingletonComponent.DisposeCalled = false;
                    AnotherSingletonComponent.DisposeCalled = false;

                    builder.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);
                    builder.RegisterSingleton(typeof(AnotherSingletonComponent),new AnotherSingletonComponent());

                    builder.Build(typeof(SingletonComponent));
                    builder.Build(typeof(AnotherSingletonComponent));
                    builder.Dispose();

                    Assert.True(SingletonComponent.DisposeCalled, "Dispose should be called on SingletonComponent");
                    Assert.True(AnotherSingletonComponent.DisposeCalled, "Dispose should be called on AnotherSingletonComponent");
                }, typeof(AutofacObjectBuilder), typeof(WindsorObjectBuilder), typeof(SpringObjectBuilder));


        }

        public class SingletonComponent :  IDisposable
        {
            public static bool DisposeCalled;

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        public class AnotherSingletonComponent : IDisposable
        {
            public static bool DisposeCalled;

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }
    }
}