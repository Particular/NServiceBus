#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
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
    [Obsolete(
        "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
        true)]
    public static class ConfigureFileShareDataBus
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public static DataBusExtensions<FileShareDataBus> BasePath(this DataBusExtensions<FileShareDataBus> config,
            string basePath) => throw new NotImplementedException();
    }

    public partial class Conventions
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public bool IsDataBusProperty(PropertyInfo property) => throw new NotImplementedException();
    }

    public partial class ConventionsBuilder
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty) =>
            throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete(
        "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
        true)]
    public class DataBusProperty<T> : IDataBusProperty where T : class
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public DataBusProperty() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public DataBusProperty(T value) => throw new NotImplementedException();

        [JsonIgnore]
        [XmlIgnore]
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public T Value => throw new NotImplementedException();

        [JsonIgnore]
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public Type Type => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public string Key { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public bool HasValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public void SetValue(object valueToSet) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public object GetValue() => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete(
        "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
        true)]
    public class FileShareDataBus : DataBusDefinition
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        protected internal override Type ProvidedByFeature() => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete(
        "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
        true)]
    public interface IDataBusProperty
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        string Key { get; set; }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        bool HasValue { get; set; }

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        object GetValue();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        void SetValue(object value);

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        Type Type { get; }
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete(
        "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
        true)]
    public class SystemJsonDataBusSerializer : IDataBusSerializer
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public void Serialize(object dataBusProperty, Stream stream) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public object Deserialize(Type propertyType, Stream stream) => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public string ContentType => throw new NotImplementedException();
    }

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete(
        "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
        true)]
    public static class UseDataBusExtensions
    {
        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition, TDataBusSerializer>(
            this EndpointConfiguration config)
            where TDataBusDefinition : DataBusDefinition, new()
            where TDataBusSerializer : IDataBusSerializer, new() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition>(
            this EndpointConfiguration config, IDataBusSerializer dataBusSerializer)
            where TDataBusDefinition : DataBusDefinition, new() => throw new NotImplementedException();

        [ObsoleteMetadata(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        [Obsolete(
            "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.",
            true)]
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config,
            Func<IServiceProvider, IDataBus> dataBusFactory, IDataBusSerializer dataBusSerializer) =>
            throw new NotImplementedException();
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

    [ObsoleteMetadata(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    [Obsolete("The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'. Will be removed in version 11.0.0.", true)]
    public class DataBus
    {
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member