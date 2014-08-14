﻿#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Text;
    using Serialization;
    using Serializers.Json;
    using Settings;

        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Json>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class JsonSerializerConfigurationExtensions
    {
        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Json>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure Json(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(b => b.UseSerialization<Bson>())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure Bson(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the encoding of JSON stream
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="encoding">Encoding to use for serialization and deserialization</param>
        public static void JsonEncoding(this SerializationConfiguration config, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            config.settings.SetProperty<JsonMessageSerializer>(s => s.Encoding, encoding);
        }
    }
}