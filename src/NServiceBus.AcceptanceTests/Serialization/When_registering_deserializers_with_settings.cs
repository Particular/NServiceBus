namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using MessageInterfaces;
    using NServiceBus.Serialization;
    using NUnit.Framework;
    using Settings;

    public class When_registering_deserializers_with_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_override_serializer_settings()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<XmlCustomSerializationReceiver>(b => b.When(
                    (session, c) =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.SetHeader("ContentType", "MyCustomSerializer");
                        return session.SendLocal(new MyRequest());
                    }))
                .WithEndpoint<XmlCustomSerializationReceiver>()
                .Done(c => c.DeserializeCalled)
                .Run();

            Assert.True(context.HandlerGotTheRequest);
            Assert.True(context.SerializeCalled);
            Assert.True(context.DeserializeCalled);
            Assert.AreEqual("SomeFancySettingsForMainSerializer", context.ValueFromSettingsForMainSerializer);
            Assert.AreEqual("SomeFancySettingsForDeserializer", context.ValueFromSettingsForDeserializer);
        }

        class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
            public bool SerializeCalled { get; set; }
            public bool DeserializeCalled { get; set; }
            public string ValueFromSettingsForMainSerializer { get; set; }
            public string ValueFromSettingsForDeserializer { get; set; }
        }

        class XmlCustomSerializationReceiver : EndpointConfigurationBuilder
        {
            public XmlCustomSerializationReceiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<MyCustomSerializer>().Settings("SomeFancySettingsForMainSerializer");
                    c.AddDeserializer<MyCustomSerializer>().Settings("SomeFancySettingsForDeserializer");
                });
            }

            class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    Context.HandlerGotTheRequest = true;
                    Context.DeserializeCalled = request.DeserializerCalled;
                    Context.SerializeCalled = request.SerializerCalled;
                    Context.ValueFromSettingsForMainSerializer = request.ValueFromSettingsForMainSerializer;
                    Context.ValueFromSettingsForDeserializer = request.ValueFromSettingsForDeserializer;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        class MyRequest : IMessage
        {
            public bool DeserializerCalled { get; set; }
            public bool SerializerCalled { get; set; }
            public string ValueFromSettingsForMainSerializer { get; set; }
            public string ValueFromSettingsForDeserializer { get; set; }
        }


        public class MyCustomSerializer : SerializationDefinition
        {
            public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
            {
                return mapper => new MyCustomMessageSerializer(settings.GetOrDefault<string>("MyCustomSerializer.Settings"));
            }
        }

        class MyCustomMessageSerializer : IMessageSerializer
        {
            readonly string valueFromSettings;

            public MyCustomMessageSerializer(string valueFromSettings)
            {
                this.valueFromSettings = valueFromSettings;
            }

            public void Serialize(object message, Stream stream)
            {
                var serializer = new BinaryFormatter();

                ((MyRequest)message).SerializerCalled = true;
                ((MyRequest)message).ValueFromSettingsForMainSerializer = valueFromSettings;

                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new BinaryFormatter();

                stream.Position = 0;
                var msg = serializer.Deserialize(stream);
                ((MyRequest)msg).DeserializerCalled = true;
                ((MyRequest)msg).ValueFromSettingsForDeserializer = valueFromSettings;
                return new[]
                {
                    msg
                };
            }

            public string ContentType => "MyCustomSerializer";
        }
    }

    static class CustomSettingsForMyCustomSerializer2
    {
        public static SerializationExtensions<When_registering_deserializers_with_settings.MyCustomSerializer> Settings(this SerializationExtensions<When_registering_deserializers_with_settings.MyCustomSerializer> extensions, string valueFromSettings)
        {
            extensions.GetSettings().Set("MyCustomSerializer.Settings", valueFromSettings);
            return extensions;
        }
    }
}