namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.DataBus;
    using NUnit.Framework;

    [TestFixture]
    public class DataBusConfigurationTests
    {
        [Test]
        public void Should_require_data_bus_type_to_implement_interface()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

            Assert.Throws<ArgumentException>(() => endpointConfiguration.UseDataBus(typeof(string), new MySerializer()));
        }

        [Test]
        public void Should_allow_multiple_deserializers_to_be_used()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

            endpointConfiguration.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>()
                .AddDeserializer(new FakeDataBusSerializer("content-type-1"))
                .AddDeserializer(new FakeDataBusSerializer("content-type-2"));

            Assert.AreEqual(endpointConfiguration.Settings.Get<List<IDataBusSerializer>>(NServiceBus.Features.DataBus.AdditionalDataBusDeserializersKey).Count, 2);
        }

        [Test]
        public void Should_not_allow_duplicate_deserializers()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");
            var config = endpointConfiguration.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>()
                .AddDeserializer(new FakeDataBusSerializer("duplicate"));

            Assert.Throws<ArgumentException>(() => config.AddDeserializer(new FakeDataBusSerializer("duplicate")));
        }

        [Test]
        public void Should_not_allow_duplicate_deserializer_with_same_content_type_as_main_serializer()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");
            var config = endpointConfiguration.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>();

            Assert.Throws<ArgumentException>(() => config.AddDeserializer<SystemJsonDataBusSerializer>());
        }

        class MySerializer : IDataBusSerializer
        {
            public string ContentType => throw new NotImplementedException();

            public object Deserialize(Type propertyType, Stream stream) => throw new NotImplementedException();
            public void Serialize(object databusProperty, Stream stream) => throw new NotImplementedException();
        }

        class MyDataBus : IDataBus
        {
            public Task<Stream> Get(string key, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<string> Put(Stream stream, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task Start(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }
    }
}
