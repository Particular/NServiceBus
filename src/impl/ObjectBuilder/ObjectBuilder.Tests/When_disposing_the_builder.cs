namespace ObjectBuilder.Tests
{
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_disposing_the_builder : BuilderFixture
    {
        [Test]
        public void Should_dispose_all_IDisposable_components()
        {
            ForAllBuilders(builder =>
                {
                    builder.Configure(typeof(SingletonComponent), DependencyLifecycle.SingleInstance);
                    builder.RegisterSingleton(typeof(AnotherSingletonComponent),new AnotherSingletonComponent());

                    builder.Build(typeof(SingletonComponent));
                    builder.Build(typeof(AnotherSingletonComponent));
                    builder.Dispose();

                    Assert.True(SingletonComponent.DisposeCalled, "Dispose should be called on SingletonComponent");
                    Assert.True(AnotherSingletonComponent.DisposeCalled, "Dispose should be called on AnotherSingletonComponent");
                });
        }
    }
}