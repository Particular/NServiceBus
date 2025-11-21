#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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

    [ObsoleteMetadata(ReplacementTypeOrMember = "Use AddHandler<TMessageHandler>(); to control order of handler invocation.", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
    [Obsolete("Use 'Use AddHandler<TMessageHandler>(); to control order of handler invocation.' instead. Will be removed in version 11.0.0.", true)]
    public static class LoadMessageHandlersExtensions
    {
        [ObsoleteMetadata(ReplacementTypeOrMember = "Use AddHandler<TMessageHandler>(); to control order of handler invocation.", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'Use AddHandler<TMessageHandler>(); to control order of handler invocation.' instead. Will be removed in version 11.0.0.", true)]
        public static void ExecuteTheseHandlersFirst(this EndpointConfiguration config, IEnumerable<Type> handlerTypes) => throw new NotImplementedException();

        [ObsoleteMetadata(ReplacementTypeOrMember = "Use AddHandler<TMessageHandler>(); to control order of handler invocation.", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'Use AddHandler<TMessageHandler>(); to control order of handler invocation.' instead. Will be removed in version 11.0.0.", true)]
        public static void ExecuteTheseHandlersFirst(this EndpointConfiguration config, params Type[] handlerTypes) => throw new NotImplementedException();
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
            Message = "It is no longer possible to enable features by default on the settings. Features can enable other features by calling EnableByDefault<T> in the constructor",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("It is no longer possible to enable features by default on the settings. Features can enable other features by calling EnableByDefault<T> in the constructor. Will be removed in version 11.0.0.", true)]
        public static SettingsHolder EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "It is no longer possible to enable features by default on the settings. Features can enable other features by calling EnableByDefault<T> in the constructor",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete("It is no longer possible to enable features by default on the settings. Features can enable other features by calling EnableByDefault<T> in the constructor. Will be removed in version 11.0.0.", true)]
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

        [ObsoleteMetadata(ReplacementTypeOrMember = "MessageHandler<THandler, TMessage>", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use 'MessageHandler<THandler, TMessage>' instead. Will be removed in version 11.0.0.", true)]
        public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
            => throw new NotImplementedException();
    }
}

namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
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
}

namespace NServiceBus.Sagas
{
    using System;
    using Particular.Obsoletes;

    public partial class SagaMetadata
    {
        [ObsoleteMetadata(Message = "Use SagaMetadata.Create to create metadata objects", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use SagaMetadata.Create to create metadata objects. Will be removed in version 11.0.0.", true)]
        public SagaMetadata(string name, System.Type sagaType, string entityName, System.Type sagaEntityType, NServiceBus.Sagas.SagaMetadata.CorrelationPropertyMetadata correlationProperty, System.Collections.Generic.IReadOnlyCollection<NServiceBus.Sagas.SagaMessage> messages, System.Collections.Generic.IReadOnlyCollection<NServiceBus.Sagas.SagaFinderDefinition> finders) { }

        [ObsoleteMetadata(Message = "Use the overload without available types and conventions", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use the overload without available types and conventions. Will be removed in version 11.0.0.", true)]
        public static NServiceBus.Sagas.SagaMetadata Create(System.Type sagaType, System.Collections.Generic.IEnumerable<System.Type> availableTypes, NServiceBus.Conventions conventions) => throw new NotImplementedException();
    }

    public partial class SagaMetadataCollection
    {
        [ObsoleteMetadata(Message = "Use the overload without available types and conventions", RemoveInVersion = "11", TreatAsErrorFromVersion = "10")]
        [Obsolete("Use the overload without available types and conventions. Will be removed in version 11.0.0.", true)]
        public void Initialize(System.Collections.Generic.IEnumerable<System.Type> availableTypes, NServiceBus.Conventions conventions) => throw new NotImplementedException();
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member