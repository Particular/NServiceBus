namespace NServiceBus
{
    using System;
    using Features;
    using NServiceBus.Settings;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the BSON serializer
    /// </summary>
    public class Bson : SerializationDefinition
    {
        /// <summary>
        /// Initialise a new instance of <see cref="Bson"/>.
        /// </summary>
        /// <param name="settings"></param>
        public Bson(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>
        /// </summary>
        internal override Type ProvidedByFeature()
        {
            return typeof(BsonSerialization);
        }

    }
}