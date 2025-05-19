#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.Json.Serialization;
    using System.Xml.Serialization;
    using NServiceBus.DataBus;

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public static class ConfigureFileShareDataBus
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public static DataBusExtensions<FileShareDataBus> BasePath(this DataBusExtensions<FileShareDataBus> config, string basePath) => throw new NotImplementedException();
    }

    public partial class Conventions
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public bool IsDataBusProperty(PropertyInfo property) => throw new NotImplementedException();
    }

    public partial class ConventionsBuilder
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty) => throw new NotImplementedException();
    }

    [ObsoleteEx(
       Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
       RemoveInVersion = "11",
       TreatAsErrorFromVersion = "10")]
    public class DataBusProperty<T> : IDataBusProperty where T : class
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public DataBusProperty() => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public DataBusProperty(T value) => throw new NotImplementedException();

        [JsonIgnore]
        [XmlIgnore]
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public T Value => throw new NotImplementedException();

        [JsonIgnore]
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public Type Type => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public string Key { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public bool HasValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public void SetValue(object valueToSet) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public object GetValue() => throw new NotImplementedException();
    }

    [ObsoleteEx(
       Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
       RemoveInVersion = "11",
       TreatAsErrorFromVersion = "10")]
    public class FileShareDataBus : DataBusDefinition
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        protected internal override Type ProvidedByFeature() => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public interface IDataBusProperty
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        string Key { get; set; }

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        bool HasValue { get; set; }

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        object GetValue();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        void SetValue(object value);

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        Type Type { get; }
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public class SystemJsonDataBusSerializer : IDataBusSerializer
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public void Serialize(object dataBusProperty, Stream stream) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public object Deserialize(Type propertyType, Stream stream) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public string ContentType => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public static class UseDataBusExtensions
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition, TDataBusSerializer>(this EndpointConfiguration config)
            where TDataBusDefinition : DataBusDefinition, new()
            where TDataBusSerializer : IDataBusSerializer, new() => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition>(this EndpointConfiguration config, IDataBusSerializer dataBusSerializer)
            where TDataBusDefinition : DataBusDefinition, new() => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Func<IServiceProvider, IDataBus> dataBusFactory, IDataBusSerializer dataBusSerializer) => throw new NotImplementedException();
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

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public abstract class DataBusDefinition
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        protected internal abstract Type ProvidedByFeature();
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public class DataBusExtensions<T> : DataBusExtensions where T : DataBusDefinition
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public DataBusExtensions(SettingsHolder settings) : base(settings) => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public class DataBusExtensions : ExposeSettings
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public DataBusExtensions(SettingsHolder settings) : base(settings) => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public DataBusExtensions AddDeserializer<TSerializer>() where TSerializer : IDataBusSerializer, new() => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        public DataBusExtensions AddDeserializer<TSerializer>(TSerializer serializer) where TSerializer : IDataBusSerializer => throw new NotImplementedException();
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public interface IDataBus
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        Task<Stream> Get(string key, CancellationToken cancellationToken = default);

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        Task<string> Put(Stream stream, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default);

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        Task Start(CancellationToken cancellationToken = default);
    }

    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public interface IDataBusSerializer
    {
        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        void Serialize(object databusProperty, Stream stream);

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        object Deserialize(Type propertyType, Stream stream);

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        string ContentType { get; }
    }
}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
    public class DataBus : Feature
    {
        internal DataBus() => throw new NotImplementedException();

        [ObsoleteEx(
            Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck'",
            RemoveInVersion = "11",
            TreatAsErrorFromVersion = "10")]
        protected internal override void Setup(FeatureConfigurationContext context) => throw new NotImplementedException();
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member