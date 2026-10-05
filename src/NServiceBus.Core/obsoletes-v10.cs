#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text.Json.Serialization;
    using System.Xml.Serialization;
    using Configuration.AdvancedExtensibility;
    using NServiceBus.DataBus;
    using Particular.Obsoletes;
    using Settings;

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public static class ConfigureFileShareDataBus
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public static DataBusExtensions<FileShareDataBus> BasePath(this DataBusExtensions<FileShareDataBus> config, string basePath) => throw new NotImplementedException();
    }

    public partial class Conventions
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public bool IsDataBusProperty(PropertyInfo property) => throw new NotImplementedException();
    }

    public partial class ConventionsBuilder
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty) => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class DataBusProperty<T> : IDataBusProperty where T : class
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public DataBusProperty() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public DataBusProperty(T value) => throw new NotImplementedException();

        [JsonIgnore]
        [XmlIgnore]
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public T Value => throw new NotImplementedException();

        [JsonIgnore]
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public Type Type => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public string Key { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public bool HasValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public void SetValue(object valueToSet) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public object GetValue() => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class FileShareDataBus : DataBusDefinition
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        protected internal override Type ProvidedByFeature() => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public interface IDataBusProperty
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        string Key { get; set; }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        bool HasValue { get; set; }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        object GetValue();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        void SetValue(object value);

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        Type Type { get; }
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class SystemJsonDataBusSerializer : IDataBusSerializer
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public void Serialize(object dataBusProperty, Stream stream) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public object Deserialize(Type propertyType, Stream stream) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public string ContentType => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public static class UseDataBusExtensions
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition, TDataBusSerializer>(this EndpointConfiguration config)
            where TDataBusDefinition : DataBusDefinition, new()
            where TDataBusSerializer : IDataBusSerializer, new() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition>(this EndpointConfiguration config, IDataBusSerializer dataBusSerializer)
            where TDataBusDefinition : DataBusDefinition, new() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Func<IServiceProvider, IDataBus> dataBusFactory, IDataBusSerializer dataBusSerializer) => throw new NotImplementedException();
    }

    public static partial class PersistenceConfig
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "UsePersistence<T>", RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'UsePersistence<T>' instead. Will be removed in version 11.0.0.", true)]
        public static PersistenceExtensions UsePersistence(this EndpointConfiguration config, Type definitionType) =>
            throw new NotImplementedException();
    }

    [ObsoleteMetadata(ReplacementTypeOrMember = "PersistenceExtensions<T>", RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("Use 'PersistenceExtensions<T>' instead. Will be removed in version 11.0.0.", true)]
    public class PersistenceExtensions : ExposeSettings
    {
        public PersistenceExtensions(Type definitionType, SettingsHolder settings, Type storageType)
            : base(settings) =>
            throw new NotImplementedException();
    }

    public partial class PersistenceExtensions<T>
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "PersistenceExtensions(SettingsHolder settings, StorageType? storageType = null)", RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'PersistenceExtensions(SettingsHolder settings, StorageType? storageType = null)' instead. Will be removed in version 11.0.0.", true)]
        protected PersistenceExtensions(SettingsHolder settings, Type storageType) : base(settings) => throw new NotImplementedException();
    }

    public static partial class EndpointConfigurationExtensions
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "EnableFeature<T>(this EndpointConfiguration config)", RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'EnableFeature<T>(this EndpointConfiguration config)' instead. Will be removed in version 11.0.0.", true)]
        public static void EnableFeature(this EndpointConfiguration config, Type featureType) => throw new NotImplementedException();

        [ObsoleteMetadata(ReplacementTypeOrMember = "DisableFeature<T>(this EndpointConfiguration config)", RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'DisableFeature<T>(this EndpointConfiguration config)' instead. Will be removed in version 11.0.0.", true)]
        public static void DisableFeature(this EndpointConfiguration config, Type featureType) => throw new NotImplementedException();
    }

    [ObsoleteMetadata(Message = "Use AddHandler<TMessageHandler>(); to control order of handler invocation.", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
    [Obsolete("Use AddHandler<TMessageHandler>(); to control order of handler invocation. Will be removed in version 11.0.0.", true)]
    public static class LoadMessageHandlersExtensions
    {
        [ObsoleteMetadata(Message = "Use AddHandler<TMessageHandler>(); to control order of handler invocation.", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use AddHandler<TMessageHandler>(); to control order of handler invocation. Will be removed in version 11.0.0.", true)]
        public static void ExecuteTheseHandlersFirst(this EndpointConfiguration config, IEnumerable<Type> handlerTypes) => throw new NotImplementedException();

        [ObsoleteMetadata(Message = "Use AddHandler<TMessageHandler>(); to control order of handler invocation.", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use AddHandler<TMessageHandler>(); to control order of handler invocation. Will be removed in version 11.0.0.", true)]
        public static void ExecuteTheseHandlersFirst(this EndpointConfiguration config, params Type[] handlerTypes) => throw new NotImplementedException();
    }

    public partial class SagaPropertyMapper<TSagaData>
    {
        [ObsoleteMetadata(Message = "The old API for mapping messages to sagas using headers has been obsoleted, use 'mapper.MapSaga(...).ToMessageHeader<MyMessage>(...)' instead", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("The old API for mapping messages to sagas using headers has been obsoleted, use 'mapper.MapSaga(...).ToMessageHeader<MyMessage>(...)' instead. Will be removed in version 11.0.0.", true)]
        public NServiceBus.IToSagaExpression<TSagaData> ConfigureHeaderMapping<TMessage>(string headerName) => throw new NotImplementedException();

        [ObsoleteMetadata(Message = "The old API for mapping messages to sagas has been obsoleted, use 'mapper.MapSaga(...).ToMessage<MyMessage>(...)' instead", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("The old API for mapping messages to sagas has been obsoleted, use 'mapper.MapSaga(...).ToMessage<MyMessage>(...)' instead. Will be removed in version 11.0.0.", true)]
        public NServiceBus.ToSagaExpression<TSagaData, TMessage> ConfigureMapping<TMessage>(System.Linq.Expressions.Expression<System.Func<TMessage, object?>> messageProperty) => throw new NotImplementedException();
    }

    [ObsoleteMetadata(Message = "The old saga mapping API has been obsoleted, use 'mapper.MapSaga(...)' instead", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
    [Obsolete("The old saga mapping API has been obsoleted, use 'mapper.MapSaga(...)' instead. Will be removed in version 11.0.0.", true)]
    public interface IToSagaExpression<TSagaData> where TSagaData : IContainSagaData
    {
        void ToSaga(System.Linq.Expressions.Expression<System.Func<TSagaData, object>> sagaEntityProperty);
    }

    [ObsoleteMetadata(Message = "The old saga mapping API has been obsoleted, use 'mapper.MapSaga(...)' instead", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
    [Obsolete("The old saga mapping API has been obsoleted, use 'mapper.MapSaga(...)' instead. Will be removed in version 11.0.0.", true)]
    public class ToSagaExpression<TSagaData, TMessage> where TSagaData : class, IContainSagaData
    {
        public ToSagaExpression(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TMessage, object>> messageProperty) => throw new NotImplementedException();
        public void ToSaga(Expression<Func<TSagaData, object?>> sagaEntityProperty) => throw new NotImplementedException();
    }

    public static class OpenTelemetryConfigurationExtensions
    {
        [ObsoleteMetadata(
            Message = "OpenTelemetry is now enabled by default. This method is no longer required",
            TreatAsErrorFromVersion = "10",
            RemoveInVersion = "11")]
        [Obsolete("OpenTelemetry is now enabled by default. This method is no longer required. Will be removed in version 11.0.0.", true)]
        public static void EnableOpenTelemetry(this EndpointConfiguration endpointConfiguration) => throw new NotImplementedException();
    }
}

namespace NServiceBus.DataBus
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Settings;
    using Particular.Obsoletes;

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public abstract class DataBusDefinition
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        protected internal abstract Type ProvidedByFeature();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class DataBusExtensions<T> : DataBusExtensions where T : DataBusDefinition
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public DataBusExtensions(SettingsHolder settings) : base(settings) => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class DataBusExtensions : ExposeSettings
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public DataBusExtensions(SettingsHolder settings) : base(settings) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public DataBusExtensions AddDeserializer<TSerializer>() where TSerializer : IDataBusSerializer, new() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        public DataBusExtensions AddDeserializer<TSerializer>(TSerializer serializer) where TSerializer : IDataBusSerializer => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public interface IDataBus
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        Task<Stream> Get(string key, CancellationToken cancellationToken = default);

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        Task<string> Put(Stream stream, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default);

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        Task Start(CancellationToken cancellationToken = default);
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public interface IDataBusSerializer
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        void Serialize(object databusProperty, Stream stream);

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        object Deserialize(Type propertyType, Stream stream);

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
        string ContentType { get; }
    }
}

namespace NServiceBus.Features
{
    using System;
    using Particular.Obsoletes;
    using Settings;

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class DataBus;

    public static partial class SettingsExtensions
    {
        [ObsoleteMetadata(
            Message = "It is no longer possible to enable features by default on the settings. Features can enable other features by calling Enable<T> in the constructor. Enabling a feature outside the context of another feature can be done by calling EnableFeature<T> on the endpoint configuration or settings",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("It is no longer possible to enable features by default on the settings. Features can enable other features by calling Enable<T> in the constructor. Enabling a feature outside the context of another feature can be done by calling EnableFeature<T> on the endpoint configuration or settings. Will be removed in version 11.0.0.", true)]
        public static SettingsHolder EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "It is no longer possible to enable features by default on the settings. Features can enable other features by calling Enable<T> in the constructor. Enabling a feature outside the context of another feature can be done by calling EnableFeature<T> on the endpoint configuration or settings",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("It is no longer possible to enable features by default on the settings. Features can enable other features by calling Enable<T> in the constructor. Enabling a feature outside the context of another feature can be done by calling EnableFeature<T> on the endpoint configuration or settings. Will be removed in version 11.0.0.", true)]
        public static SettingsHolder EnableFeatureByDefault(this SettingsHolder settings, Type featureType) => throw new NotImplementedException();

        [ObsoleteMetadata(
            ReplacementTypeOrMember = "IsFeatureActive<T>(this IReadOnlySettings settings)",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'IsFeatureActive<T>(this IReadOnlySettings settings)' instead. Will be removed in version 11.0.0.", true)]
        public static bool IsFeatureActive(this IReadOnlySettings settings, Type featureType) => throw new NotImplementedException();

        [ObsoleteMetadata(
            ReplacementTypeOrMember = "IsFeatureEnabled<T>(this IReadOnlySettings settings)",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'IsFeatureEnabled<T>(this IReadOnlySettings settings)' instead. Will be removed in version 11.0.0.", true)]
        public static bool IsFeatureEnabled(this IReadOnlySettings settings, Type featureType) => throw new NotImplementedException();
    }

    public abstract partial class Feature
    {
        [ObsoleteMetadata(
            ReplacementTypeOrMember = "DependsOnOptionally<T>()",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'DependsOnOptionally<T>()' instead. Will be removed in version 11.0.0.", true)]
        protected void DependsOnOptionally(Type featureType) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Persistence
{
    using System;
    using Particular.Obsoletes;
    using Settings;

    public partial class PersistenceDefinition
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "Supports<TStorage, TFeature>()", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'Supports<TStorage, TFeature>()' instead. Will be removed in version 11.0.0.", true)]
        protected void Supports<T>(Action<SettingsHolder> action) where T : StorageType => throw new NotImplementedException();

        [ObsoleteMetadata(ReplacementTypeOrMember = "HasSupportFor<T>()", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'HasSupportFor<T>()' instead. Will be removed in version 11.0.0.", true)]
        public bool HasSupportFor(Type storageType) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using Particular.Obsoletes;

    public partial class MessageHandler
    {
        public MessageHandler()
        {
            // Won't be needed once the obsolete member is removed.
        }

        [ObsoleteMetadata(ReplacementTypeOrMember = "MessageHandler()", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'MessageHandler()' instead. Will be removed in version 11.0.0.", true)]
        public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
            => throw new NotImplementedException();
    }
}

namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.Obsoletes;

    public partial class SagaFinderDefinition
    {
        [ObsoleteMetadata(Message = "Use MessageType.FullName instead", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use MessageType.FullName instead. Will be removed in version 11.0.0.", true)]
        public string MessageTypeName { get; }

        [ObsoleteMetadata(Message = "Finder properties are no longer used", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Finder properties are no longer used. Will be removed in version 11.0.0.", true)]
        public Dictionary<string, object> Properties { get; }
    }

    [ObsoleteMetadata(Message = "Saga not found handlers are no longer automatically registered during assembly scanning. Handlers are no longer global and should be registered for each saga using mapper.ConfigureNotFoundHandler<MyNotFoundHandler>()", RemoveInVersion = "11", TreatAsErrorFromVersion = "10", ReplacementTypeOrMember = "ISagaNotFoundHandler")]
    [Obsolete("Saga not found handlers are no longer automatically registered during assembly scanning. Handlers are no longer global and should be registered for each saga using mapper.ConfigureNotFoundHandler<MyNotFoundHandler>(). Use 'ISagaNotFoundHandler' instead. Will be removed in version 11.0.0.", true)]
    public interface IHandleSagaNotFound
    {
        Task Handle(object message, IMessageProcessingContext context);
    }
}

namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using Particular.Obsoletes;

    public partial class SagaMetadata
    {
        [ObsoleteMetadata(Message = "Use SagaMetadata.Create to create metadata objects", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use SagaMetadata.Create to create metadata objects. Will be removed in version 11.0.0.", true)]
        public SagaMetadata(string name, System.Type sagaType, string entityName, System.Type sagaEntityType, NServiceBus.Sagas.SagaMetadata.CorrelationPropertyMetadata correlationProperty, System.Collections.Generic.IReadOnlyCollection<NServiceBus.Sagas.SagaMessage> messages, System.Collections.Generic.IReadOnlyCollection<NServiceBus.Sagas.SagaFinderDefinition> finders) { }

        [ObsoleteMetadata(Message = "Use the overload without available types and conventions", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use the overload without available types and conventions. Will be removed in version 11.0.0.", true)]
        public static NServiceBus.Sagas.SagaMetadata Create(System.Type sagaType, System.Collections.Generic.IEnumerable<System.Type> availableTypes, NServiceBus.Conventions conventions) => throw new NotImplementedException();

        [ObsoleteMetadata(ReplacementTypeOrMember = "Create<TSagaType>()", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'Create<TSagaType>()' instead. Will be removed in version 11.0.0.", true)]
        public static NServiceBus.Sagas.SagaMetadata Create(System.Type sagaType) => throw new NotImplementedException();
    }

    public partial class SagaMetadataCollection
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "SagaMetadata.CreateMany", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'SagaMetadata.CreateMany' instead. Will be removed in version 11.0.0.", true)]
        public void Initialize(System.Collections.Generic.IEnumerable<System.Type> availableTypes, NServiceBus.Conventions conventions) => throw new NotImplementedException();

        [ObsoleteMetadata(ReplacementTypeOrMember = "SagaMetadata.CreateMany", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'SagaMetadata.CreateMany' instead. Will be removed in version 11.0.0.", true)]
        public void Initialize(IEnumerable<Type> availableTypes) => throw new NotImplementedException();
    }
}

namespace NServiceBus.Unicast.Messages
{
    using System;
    using Particular.Obsoletes;

    public partial class MessageMetadataRegistry
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "MessageMetadataRegistry.Initialize", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'MessageMetadataRegistry.Initialize' instead. Will be removed in version 11.0.0.", true)]
        public MessageMetadataRegistry(Func<Type, bool> isMessageType, bool allowDynamicTypeLoading)
        {
        }
    }
}

namespace NServiceBus
{
#nullable enable
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    // =============================================================================
    // OPENTELEMETRY V11 BEHAVIOR OPT-IN. EVERYTHING IN THIS BLOCK IS REMOVED IN v11.
    //
    // Several OpenTelemetry behaviors become the one and only behavior in v11, with no
    // option left to opt in or out. Until then an endpoint can adopt them as a whole on
    // v10 through a single AppContext switch:
    //
    //   AppContext.SetSwitch("NServiceBus.Core.OpenTelemetry.UseV11Behavior", true);
    //
    // or in runtimeconfig.template.json / the project file:
    //
    //   <ItemGroup>
    //     <RuntimeHostConfigurationOption Include="NServiceBus.Core.OpenTelemetry.UseV11Behavior" Value="true" />
    //   </ItemGroup>
    //
    // With the switch enabled:
    // - Trace context and baggage are propagated through System.Diagnostics.DistributedContextPropagator
    //   (W3C baggage encoding) instead of LegacyContextPropagation below. The baggage wire format changes
    //   (W3C OWS encoding + whitespace trimming), which is breaking on rolling upgrades.
    //   https://github.com/Particular/NServiceBus/issues/7825
    // - Handler spans are emitted from the dedicated "NServiceBus.Core.Handler" ActivitySource instead of
    //   "NServiceBus.Core" so they can be filtered/sampled independently. Existing OpenTelemetry
    //   configurations that only subscribe to "NServiceBus.Core" would silently lose handler spans.
    //   https://github.com/Particular/NServiceBus/issues/7284
    // - When a transport SDK (Azure Service Bus, RabbitMQ, SQS, ...) has its own OpenTelemetry
    //   instrumentation, its ambient "receive" span becomes the parent of the incoming message span and
    //   the NServiceBus sender span is linked instead of being the parent. This changes trace shapes.
    // - Span names follow the OTel messaging convention `{messaging.operation.name} {destination}`,
    //   e.g. "process orders", "send Payment", "publish OrderPlaced", "immediate retry orders".
    // - The "Start dispatching"/"Finished dispatching" events are no longer added to the incoming span
    //   when outgoing messages are dispatched.
    // - The legacy `execution.result` tag ("success"/"failure") is no longer emitted on the handler time,
    //   processing time, saga fetch time, deserialize time, and serialize time metrics.
    // - The legacy `otel.status_code`/`otel.status_description` tags (redundant with Activity.Status) and
    //   the deprecated `exception.escaped` exception event attribute are no longer set on failures.
    //   https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/
    // - The ActivitySources report version 1.0.0 instead of 0.1.0, so a consumer can tell the two tag and
    //   span-name sets apart.
    // - The outbox deduplication span tag is named `nservicebus.outbox.deduplicated_message` instead of
    //   `nservicebus.outbox.deduplicate-message`, following the OpenTelemetry attribute naming rules
    //   (snake_case within a dot-delimited component, no hyphens).
    //   https://opentelemetry.io/docs/specs/semconv/general/naming/
    // - The `nservicebus.event_types` tag on subscribe and unsubscribe spans and the
    //   `nservicebus.enclosed_message_types` tag on message spans are arrays of full type names instead of
    //   delimited strings. The OpenTelemetry naming rules ask for an array when an attribute holds several
    //   values. Array-valued tags are only visible through Activity.TagObjects, not Activity.Tags.
    //
    // In v11: delete this entire namespace block, search the code base for `V11BehaviorSwitch` and keep
    // only the branch each check guards for the enabled case. ActivityFactory, ActivitySources, ContextPropagation,
    // MessageOperations, RoutingToDispatchConnector, TransportReceiveToPhysicalMessageConnector and
    // PipelineMetrics are the production call sites. Delete the pre-v11 default tests
    // (ContextPropagationDefaultBehaviorTests, LegacyContextPropagationTests, TransportParentSpanDefaultBehaviorTests,
    // the "Default_..." tests in HandlerActivitySourceTests) and the switch SetUp/TearDown pairs in the
    // remaining unit and acceptance tests, together with the OpenTelemetryV11Defaults attribute in the
    // obsoletes-v10.cs of NServiceBus.Core.Tests and NServiceBus.AcceptanceTests.
    // =============================================================================
    static class V11BehaviorSwitch
    {
        enum SwitchState : byte
        {
            Unchecked = 0,
            Enabled = 1,
            Disabled = 2
        }

        static SwitchState cachedUseV11Behavior;

        public const string UseV11BehaviorSwitchName = "NServiceBus.Core.OpenTelemetry.UseV11Behavior";

        public static bool UseV11Behavior
        {
            get
            {
                var state = cachedUseV11Behavior;
                if (state != SwitchState.Unchecked)
                {
                    return state == SwitchState.Enabled;
                }

                state = AppContext.TryGetSwitch(UseV11BehaviorSwitchName, out var isEnabled) && isEnabled
                    ? SwitchState.Enabled
                    : SwitchState.Disabled;
                cachedUseV11Behavior = state;

                return state == SwitchState.Enabled;
            }
        }

        internal static void ResetUseV11Behavior() => cachedUseV11Behavior = SwitchState.Unchecked;
    }

    // The pre-v11 trace-context/baggage propagator. ContextPropagation.cs delegates here while
    // V11BehaviorSwitch.UseV11Behavior is off.
    static class LegacyContextPropagation
    {
        public static void PropagateContextToHeaders(Activity? activity, Dictionary<string, string> headers)
        {
            if (activity is null)
            {
                return;
            }

            if (activity.Id is not null)
            {
                headers[Headers.DiagnosticsTraceParent] = activity.Id;
            }

            if (activity.TraceStateString is not null)
            {
                headers[Headers.DiagnosticsTraceState] = activity.TraceStateString;
            }

            var baggage = string.Join(",", activity.Baggage.Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));
            if (!string.IsNullOrEmpty(baggage))
            {
                headers[Headers.DiagnosticsBaggage] = baggage;
            }
        }

        public static void PropagateContextFromHeaders(Activity? activity, IDictionary<string, string> headers)
        {
            if (activity is null)
            {
                return;
            }

            PropagateTraceStateFromHeaders(activity, headers);
            PropagateBaggageFromHeaders(activity, headers);
        }

        public static void PropagateTraceStateFromHeaders(Activity activity, IDictionary<string, string> headers)
        {
            if (headers.TryGetValue(Headers.DiagnosticsTraceState, out var traceState))
            {
                activity.TraceStateString = traceState;
            }
        }

        // parent: the activity whose baggage chain is checked so keys it already carries are not added again,
        // see ContextPropagation.PropagateBaggageFromHeaders.
        public static void PropagateBaggageFromHeaders(Activity activity, IDictionary<string, string> headers, Activity? parent = null)
        {
            if (!headers.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue))
            {
                return;
            }

            var baggageSpan = baggageValue.AsSpan();
            // HINT: Iterate in reverse order because Activity baggage is LIFO
            while (!baggageSpan.IsEmpty)
            {
                var lastComma = baggageSpan.LastIndexOf(',');
                ReadOnlySpan<char> baggageItem;

                if (lastComma >= 0)
                {
                    baggageItem = baggageSpan[(lastComma + 1)..];
                    baggageSpan = baggageSpan[..lastComma];
                }
                else
                {
                    baggageItem = baggageSpan;
                    baggageSpan = [];
                }

                var firstEquals = baggageItem.IndexOf('=');
                if (firstEquals < 0 || firstEquals >= baggageItem.Length)
                {
                    continue;
                }

                var key = baggageItem[..firstEquals].Trim().ToString();
                if (parent?.GetBaggageItem(key) is not null)
                {
                    continue;
                }

                var value = baggageItem[(firstEquals + 1)..];
                activity.AddBaggage(key, Uri.UnescapeDataString(value));
            }
        }
    }

    // The pre-v11 exception tagging. ActivityFactory.RecordError applies these while
    // V11BehaviorSwitch.UseV11Behavior is off.
    static class LegacyExceptionTags
    {
        public static void SetLegacyStatusTags(Activity activity, Exception exception)
        {
            activity.SetTag("otel.status_code", "ERROR");
            activity.SetTag("otel.status_description", exception.Message);
        }

        public static TagList EscapedTagList { get; } = new() { { "exception.escaped", true } };
    }

    // The pre-v11 names of span tags that were renamed to follow the OpenTelemetry naming rules.
    // TransportReceiveToPhysicalMessageConnector writes these while V11BehaviorSwitch.UseV11Behavior is off.
    static class LegacyActivityTags
    {
        public const string OutboxDeduplicateMessage = "nservicebus.outbox.deduplicate-message";
    }

    // The pre-v11 execution.result metric tag. PipelineMetrics applies it while
    // V11BehaviorSwitch.UseV11Behavior is off.
    static class LegacyExecutionResultTag
    {
        // The tag is going away, so a user cannot override it through IMetricsTags.
        public static void Add(ref TagList tags, Exception? error = null)
        {
            if (!V11BehaviorSwitch.UseV11Behavior)
            {
                tags.Add(MeterTags.ExecutionResult, error is null ? "success" : "failure");
            }
        }
    }

    public partial class InstrumentationOptions
    {
        // Publishes start a new linked trace by default in v10 for backward compatibility. This one keeps a
        // public option (PublishTraceMode) in v11 and is therefore not governed by V11BehaviorSwitch. In v11,
        // delete this method: PublishTraceMode then defaults to TraceMode.ContinueExisting through its
        // initializer in InstrumentationOptions.cs, and the empty partial declaration plus the constructor
        // call there can be removed.
        partial void ApplyPreV11Defaults() => PublishTraceMode = TraceMode.StartNew;
    }
#nullable restore
}

#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member