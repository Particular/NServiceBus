#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus.Serializers.XML.Config
{
    using System;

    [ObsoleteEx(
            Message = "Use configuration.UseSerialization<XmlSerializer>(), where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
    public class XmlSerializationSettings
    {

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>().DontWrapRawXml()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings DontWrapRawXml()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>().Namespace(namespaceToUse)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings Namespace(string namespaceToUse)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<XmlSerializer>().SanitizeInput()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public XmlSerializationSettings SanitizeInput()
        {
            throw new NotImplementedException();
        }
    }
}