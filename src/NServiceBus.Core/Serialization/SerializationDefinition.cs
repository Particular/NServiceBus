namespace NServiceBus.Serialization
{
    using System;

    /// <summary>
    /// Implemented by serializers to provide their capabilities.
    /// </summary>
    public abstract class SerializationDefinition
    {
        /// <inheritdoc />
        bool Equals(SerializationDefinition other)
        {
            return ProvidedByFeature() == other.ProvidedByFeature();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((SerializationDefinition) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ProvidedByFeature().GetHashCode();
        }

        /// <summary>
        /// The feature to enable when this serializer is selected.
        /// </summary>
        protected internal abstract Type ProvidedByFeature();
    }
}