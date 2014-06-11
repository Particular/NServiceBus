namespace NServiceBus.Core.Tests.DataBus
{
    using System;
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
            var config = Configure.With(o =>
            {
                o.EndpointName("xyz");
                o.TypesToScan(new[]
                {
                    typeof(MessageWithDataBusProperty)
                });
            });

            var feature = new DataBusFeature();

            config.Configurer.ConfigureComponent<InMemoryDataBus>(DependencyLifecycle.SingleInstance);

            Assert.True(feature.ShouldBeSetup(new FeatureConfigurationContext(config)));
        }

        [Test]
        public void Databus_should_not_be_activated_if_no_databus_property_is_found()
        {
            var config = Configure.With(o =>
            {
                o.EndpointName("xyz");
                o.TypesToScan(new[]
                {
                    typeof(MessageWithoutDataBusProperty)
                });
            });
            var feature = new DataBusFeature();


            Assert.False(feature.ShouldBeSetup(new FeatureConfigurationContext(config)));
        }

        [Test]
        public void Should_throw_if_propertyType_is_not_serializable()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Assert.Ignore("This only work in debug mode.");
            }

            var config = Configure.With(o =>
            {
                o.EndpointName("xyz");
                o.TypesToScan(new[]
                {
                    typeof(MessageWithNonSerializableDataBusProperty)
                });
            })
                .DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"));

            var feature = new DataBusFeature();

            Assert.Throws<InvalidOperationException>(() => feature.ShouldBeSetup(new FeatureConfigurationContext(config)));
        }

        [Test]
        public void Should_not_throw_propertyType_is_not_serializable_if_a_IDataBusSerializer_is_already_registered()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Assert.Ignore("This only work in debug mode.");
            }

            var config = Configure.With(o =>
            {
                o.EndpointName("xyz");
                o.TypesToScan(new[]
                {
                    typeof(MessageWithNonSerializableDataBusProperty)
                });
            })
                .DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"));
            
            config.Configurer.RegisterSingleton<IDataBus>(new InMemoryDataBus());

            var feature = new DataBusFeature();

            config.Configurer.ConfigureComponent<IDataBusSerializer>(() => new MyDataBusSerializer(), DependencyLifecycle.SingleInstance);

            Assert.DoesNotThrow(() => feature.ShouldBeSetup(new FeatureConfigurationContext(config)));
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
