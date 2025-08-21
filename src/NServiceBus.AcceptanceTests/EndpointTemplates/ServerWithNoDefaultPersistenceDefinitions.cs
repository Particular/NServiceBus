namespace NServiceBus.AcceptanceTests.EndpointTemplates;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using MessageInterfaces;
using NServiceBus.Serialization;
using Settings;
using Unicast.Messages;
using ProtoBuf.Meta;
using NServiceBus.Configuration.AdvancedExtensibility;
using NUnit.Framework.Internal;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

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

        builder.UseSerialization<ProtoBufSerializer>();
        //var serializer = builder.UseSerialization<ProtoBufSerializer>();
        //serializer.RuntimeTypeModel(RuntimeTypeModel.Default);

        await configurationBuilderCustomization(builder);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        builder.ScanTypesForTest(endpointConfiguration);

        return builder;
    }
}

public class ProtoBufSerializer :
    SerializationDefinition
{
    /// <summary>
    /// <see cref="SerializationDefinition.Configure"/>
    /// </summary>
    public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
    {
        var runtimeTypeModel = settings.GetRuntimeTypeModel();

        if (runtimeTypeModel == null)
        {
            runtimeTypeModel = RuntimeTypeModel.Create();

            var registry = settings.Get<MessageMetadataRegistry>();
            var messageTypes = registry.GetAllMessages().Select(m => m.MessageType);

            foreach (var messageType in messageTypes)
            {
                var metaType = runtimeTypeModel.Add(messageType, false);
                var propertyCounter = 1;
                var props = messageType.GetProperties();
                foreach (var prop in props)
                {
                    metaType.Add(propertyCounter++, prop.Name);
                }
            }
        }
        var contentTypeKey = settings.GetContentTypeKey();
        return _ => new ProtobufMessageSerializer(contentTypeKey, runtimeTypeModel);
    }
}

class ProtobufMessageSerializer :
    IMessageSerializer
{
    RuntimeTypeModel runtimeTypeModel;

    public ProtobufMessageSerializer(string? contentType, RuntimeTypeModel? runtimeTypeModel)
    {
        if (runtimeTypeModel == null)
        {
            this.runtimeTypeModel = RuntimeTypeModel.Default;
        }
        else
        {
            this.runtimeTypeModel = runtimeTypeModel;
        }

        if (contentType == null)
        {
            ContentType = "protobuf";
        }
        else
        {
            ContentType = contentType;
        }
    }

    public void Serialize(object message, Stream stream)
    {
        var messageType = message.GetType();
        if (messageType.Name.EndsWith("__impl"))
        {
            throw new("Interface based message are not supported. Create a class that implements the desired interface.");
        }

        runtimeTypeModel.Serialize(stream, message);
    }

    public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes)
    {
        var messageType = messageTypes.First();

        object message = null;
        using (var stream = new MemoryStream(body.ToArray(), writable: false))
        {
            message = runtimeTypeModel.Deserialize(stream, null, messageType);
        }

        return new[] { message };
    }

    public string ContentType { get; }
}

public static class ProtoBufConfigurationExtensions
{
    /// <summary>
    /// Configures the <see cref="RuntimeTypeModel"/> to use.
    /// </summary>
    /// <param name="config">The <see cref="SerializationExtensions{T}"/> instance.</param>
    /// <param name="runtimeTypeModel">The <see cref="RuntimeTypeModel"/> to use.</param>
    public static void RuntimeTypeModel(this SerializationExtensions<ProtoBufSerializer> config, RuntimeTypeModel runtimeTypeModel)
    {
        var settings = config.GetSettings();
        settings.Set(runtimeTypeModel);
    }

    internal static RuntimeTypeModel GetRuntimeTypeModel(this IReadOnlySettings settings) =>
        settings.GetOrDefault<RuntimeTypeModel>();

    /// <summary>
    /// Configures string to use for <see cref="Headers.ContentType"/> headers.
    /// </summary>
    /// <remarks>
    /// Defaults to "wire".
    /// </remarks>
    /// <param name="config">The <see cref="SerializationExtensions{T}"/> instance.</param>
    /// <param name="contentTypeKey">The content type key to use.</param>
    public static void ContentTypeKey(this SerializationExtensions<ProtoBufSerializer> config, string contentTypeKey)
    {
        Guard.AgainstEmpty(contentTypeKey, nameof(contentTypeKey));
        var settings = config.GetSettings();
        settings.Set("NServiceBus.ProtoBuf.ContentTypeKey", contentTypeKey);
    }

    internal static string GetContentTypeKey(this IReadOnlySettings settings) =>
        settings.GetOrDefault<string>("NServiceBus.ProtoBuf.ContentTypeKey");
}

static class Guard
{
    public static void AgainstEmpty(string value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(argumentName);
        }
    }
}
