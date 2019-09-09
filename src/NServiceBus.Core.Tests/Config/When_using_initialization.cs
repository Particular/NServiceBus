namespace NServiceBus.Core.Tests.Config
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_initialization_with_non_default_ctor
    {
        [Test]
        public void Should_throw_meaningful_exception()
        {
            var endpointConfiguration = new EndpointConfiguration("myendpoint");

            endpointConfiguration.TypesToScanInternal(new[] { typeof(FeatureWithInitialization) });

            var ae = Assert.Throws<Exception>(() => EndpointCreator.CreateWithInternallyManagedContainer(endpointConfiguration));
            var expected = $"Unable to create the type '{nameof(FeatureWithInitialization)}'. Types implementing '{nameof(INeedInitialization)}' must have a public parameterless (default) constructor.";
            Assert.AreEqual(expected, ae.Message);
        }

        public class FeatureWithInitialization : INeedInitialization
        {
            public FeatureWithInitialization(string arg)
            {
                // Note: this ctor will cause the builder to throw an exception.
                // If  using assembly scanning in tests, ensure to exclude this type by using:
                // endpointConfiguration.ExcludeTypes(typeof(When_using_initialization_with_non_default_ctor.FeatureWithInitialization));
            }

            public void Customize(EndpointConfiguration configuration)
            {
            }
        }

    }
}