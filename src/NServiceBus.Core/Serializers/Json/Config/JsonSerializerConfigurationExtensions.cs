﻿#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Text;
    using NServiceBus.Serialization;
    using Serializers.Json;
    using Settings;

    public static class JsonSerializerConfigurationExtensions
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<JsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static SerializationSettings Json(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseSerialization<BsonSerializer>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static SerializationSettings Bson(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the encoding of JSON stream
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="encoding">Encoding to use for serialization and deserialization</param>
        public static void Encoding(this SerializationExtentions<JsonSerializer> config, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            config.Settings.SetProperty<JsonMessageSerializer>(s => s.Encoding, encoding);
        }
    }
}