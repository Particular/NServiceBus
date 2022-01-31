namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageInterfaces;
    using NServiceBus.Serialization;
    using NUnit.Framework;
    using Settings;

    public class When_defining_serializer_with_no_content_type : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_fail_endpoint_startup()
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointWithInvalidSerializer>()
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains($"Serializer '{nameof(InvalidSerializer)}' defines no content type. Ensure the 'ContentType' property of the serializer has a value.", exception.Message);
        }

        class EndpointWithInvalidSerializer : EndpointConfigurationBuilder
        {
            public EndpointWithInvalidSerializer()
            {
                EndpointSetup<DefaultServer>(c => c.UseSerialization<InvalidSerializer>());
            }
        }

        class InvalidSerializer : SerializationDefinition, IMessageSerializer
        {
            public string ContentType => null; // equal to not setting a value at all
            public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings) => _ => this;

            public void Serialize(object message, Stream stream) => throw new NotImplementedException();

            public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null) => throw new NotImplementedException();
        }
    }
}