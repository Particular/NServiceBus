namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using NServiceBus.DataBus;
    using NServiceBus.DataBus.InMemory;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_nservicebus_is_initializing
    {
        [Test]
        public void Databus_should_be_activated_if_a_databus_property_is_found()
        {
            var builder = new ConfigurationBuilder();

            builder.EndpointName("xyz");
            builder.TypesToScan(new[]{typeof(MessageWithDataBusProperty)});

            var config = Configure.With(builder);

            var feature = new DataBusFeature();

            config.configurer.ConfigureComponent<InMemoryDataBus>(DependencyLifecycle.SingleInstance);

            Assert.True(feature.CheckPrerequisites(new FeatureConfigurationContext(config)).IsSatisfied);
        }

        [Test]
        public void Databus_should_not_be_activated_if_no_databus_property_is_found()
        {
            var builder = new ConfigurationBuilder();

            builder.EndpointName("xyz");
            builder.TypesToScan(new[] { typeof(MessageWithoutDataBusProperty) });

            var config = Configure.With(builder);

            var feature = new DataBusFeature();

            Assert.False(feature.CheckPrerequisites(new FeatureConfigurationContext(config)).IsSatisfied);
        }

        [Test]
        public void Should_throw_if_propertyType_is_not_serializable()
        {
            if (!Debugger.IsAttached)
            {
                Assert.Ignore("This only work in debug mode.");
            }

            var builder = new ConfigurationBuilder();
            builder.EndpointName("xyz");
            builder.TypesToScan(new[]
                {
                    typeof(MessageWithNonSerializableDataBusProperty)
                });
            builder.Conventions().DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"));
            
            var config = Configure.With(builder);

            var feature = new DataBusFeature();

            Assert.Throws<InvalidOperationException>(() => feature.CheckPrerequisites(new FeatureConfigurationContext(config)));
        }

        [Test]
        public void Should_not_throw_propertyType_is_not_serializable_if_a_IDataBusSerializer_is_already_registered()
        {
            if (!Debugger.IsAttached)
            {
                Assert.Ignore("This only work in debug mode.");
            }

            var builder = new ConfigurationBuilder();
            builder.EndpointName("xyz");
            builder.TypesToScan(new[]
                {
                    typeof(MessageWithNonSerializableDataBusProperty)
                });
            builder.Conventions().DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"));

            var config = Configure.With(builder);
            
            config.configurer.RegisterSingleton<IDataBus>(new InMemoryDataBus());

            var feature = new DataBusFeature();

            config.configurer.ConfigureComponent<IDataBusSerializer>(() => new MyDataBusSerializer(), DependencyLifecycle.SingleInstance);

            Assert.DoesNotThrow(() => feature.CheckPrerequisites(new FeatureConfigurationContext(config)));
        }

        class MyDataBusSerializer : IDataBusSerializer
        {
            public void Serialize(object databusProperty, Stream stream)
            {
                throw new NotImplementedException();
            }

            public object Deserialize(Stream stream)
            {
                throw new NotImplementedException();
            }
        }
    }


}
