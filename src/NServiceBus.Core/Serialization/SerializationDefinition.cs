﻿namespace NServiceBus.Serialization
{
    using System;
    using Settings;

    /// <summary>
    /// Implemented by serializers to provide their capabilities.
    /// </summary>
    public abstract class SerializationDefinition
    {
        /// <summary>
        /// Provides a factory method for building a message serializer.
        /// </summary>
        public abstract Func<IMessageSerializer> Configure(ReadOnlySettings settings);
    }
}