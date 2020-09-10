namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using MessageInterfaces;
    using NServiceBus.Serialization;
    using NUnit.Framework;
    using Settings;

    public class When_registering_additional_deserializers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Two_endpoints_with_different_serializers_should_deserialize_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<CustomSerializationSender>(b => b.When(
                    (session, c) =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.SetHeader("ContentType", "MyCustomSerializer");
                        return session.Send(new MyRequest());
                    }))
                .WithEndpoint<XmlCustomSerializationReceiver>()
                .Done(c => c.HandlerGotTheRequest)
                .Run();

            Assert.True(context.HandlerGotTheRequest);
            Assert.True(context.SerializeCalled);
            Assert.True(context.DeserializeCalled);
            Assert.AreEqual("SomeFancySettings", context.ValueFromSettings);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
            public bool SerializeCalled { get; set; }
            public bool DeserializeCalled { get; set; }
            public string ValueFromSettings { get; set; }
        }

        class CustomSerializationSender : EndpointConfigurationBuilder
        {
            public CustomSerializationSender()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<MyCustomSerializer>().Settings((Context)r.ScenarioContext, "");
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyRequest), typeof(XmlCustomSerializationReceiver));
                });
            }
        }

        class XmlCustomSerializationReceiver : EndpointConfigurationBuilder
        {
            public XmlCustomSerializationReceiver()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<XmlSerializer>();
                    c.AddDeserializer<MyCustomSerializer>().Settings((Context)r.ScenarioContext, "SomeFancySettings");
                });
            }

            class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public MyRequestHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyRequest request, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.HandlerGotTheRequest = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }

        public class MyCustomSerializer : SerializationDefinition
        {
            public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
            {
                var context = settings.Get<Context>();
                return mapper => new MyCustomMessageSerializer(context, settings.Get<string>("MyCustomSerializer.Settings.Value"));
            }
        }

        class MyCustomMessageSerializer : IMessageSerializer
        {
            public MyCustomMessageSerializer(Context context, string valueFromSettings)
            {
                this.context = context;
                this.valueFromSettings = valueFromSettings;
            }

            public void Serialize(object message, Stream stream)
            {
                var serializer = new BinaryFormatter();

                context.SerializeCalled = true;

                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new BinaryFormatter();

                stream.Position = 0;
                var msg = serializer.Deserialize(stream);
                context.DeserializeCalled = true;
                context.ValueFromSettings = valueFromSettings;

                return new[]
                {
                    msg
                };
            }

            public string ContentType => "MyCustomSerializer";
            readonly Context context;
            readonly string valueFromSettings;
        }
    }

    static class CustomSettingsForMyCustomSerializer
    {
        public static void Settings(this SerializationExtensions<When_registering_additional_deserializers.MyCustomSerializer> extensions, When_registering_additional_deserializers.Context context, string valueFromSettings)
        {
            var settings = extensions.GetSettings();
            settings.Set("MyCustomSerializer.Settings", context);
            settings.Set("MyCustomSerializer.Settings.Value", valueFromSettings);
        }
    }
}