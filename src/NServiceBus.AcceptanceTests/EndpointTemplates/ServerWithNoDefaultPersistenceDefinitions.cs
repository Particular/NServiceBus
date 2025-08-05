namespace NServiceBus.AcceptanceTests.EndpointTemplates;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Avro.IO;
using Avro.Reflect;
using Chr.Avro.Abstract;
using Chr.Avro.Representation;
using MessageInterfaces;
using NServiceBus.Serialization;
using Settings;
using Unicast.Messages;
using Schema = Avro.Schema;

public class ServerWithNoDefaultPersistenceDefinitions : IEndpointSetupTemplate
{
    public IConfigureEndpointTestExecution TransportConfiguration { get; set; } = TestSuiteConstraints.Current.CreateTransportConfiguration();

    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
        builder.EnableInstallers();

        builder.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));
        builder.SendFailedMessagesTo("error");

        await builder.DefineTransport(TransportConfiguration, runDescriptor, endpointConfiguration).ConfigureAwait(false);

        builder.UseSerialization<AvroSerializer>();

        await configurationBuilderCustomization(builder).ConfigureAwait(false);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        builder.ScanTypesForTest(endpointConfiguration);

        return builder;
    }
}

public class AvroSerializer : SerializationDefinition
{
    public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
    {
        //for now use reflection to generate the schemas
        var registry = settings.Get<MessageMetadataRegistry>();
        var messageTypes = registry.GetAllMessages().Select(m => m.MessageType);
        var schemaCache = new SchemaCache();

        foreach (var messageType in messageTypes)
        {
            var builder = new SchemaBuilder(
            nullableReferenceTypeBehavior: NullableReferenceTypeBehavior.All);
            var chrSchema = builder.BuildSchema(messageType); // a RecordSchema instance

            var writer = new JsonSchemaWriter();
            var schemaJson = writer.Write(chrSchema);

            Console.WriteLine(schemaJson);
            schemaCache.Add(messageType, Schema.Parse(schemaJson));
        }

        return _ => new AvroMessageSerializer(schemaCache, new ClassCache());
    }
}

public class AvroMessageSerializer(SchemaCache schemaCache, ClassCache classCache) : IMessageSerializer
{
    public string ContentType => "avro/binary";

    public void Serialize(object message, Stream stream)
    {
        // TODO: serializing records fails
        var schema = schemaCache.GetSchema(message.GetType());
        var writer = new ReflectDefaultWriter(message.GetType(), schema, classCache);

        writer.Write(message, new BinaryEncoder(stream));
    }

    public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null)
    {
        var messages = new List<object>();
        foreach (var messageType in messageTypes)
        {
            var schema = schemaCache.GetSchema(messageType);
            var reader = new ReflectDefaultReader(messageType, schema, schema, classCache);
            using var stream = new ReadOnlyStream(body);
            var message = reader.Read(null, schema, schema, new BinaryDecoder(stream));
            messages.Add(message);
        }

        return messages.ToArray();
    }
}

public class SchemaCache
{
    public Schema GetSchema(Type getType) => schemaCache[getType];

    readonly IDictionary<Type, Schema> schemaCache = new Dictionary<Type, Schema>();

    public void Add(Type messageType, Schema schema)
    {
        schemaCache[messageType] = schema;
    }
}