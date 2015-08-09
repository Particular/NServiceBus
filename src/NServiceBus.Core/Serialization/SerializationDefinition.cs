namespace NServiceBus.Serialization
{
    using System;

    /// <summary>
    /// Implemented by serializers to provide their capabilities.
    /// </summary>
    public abstract class SerializationDefinition
    {
        /// <summary>
        /// Indicates whether the current serialization definition is equal to another.
        /// </summary>
        /// <returns>
        /// true if the current definition is equal to the <paramref name="other"/>; otherwise, false.
        /// </returns>
        /// <param name="other">A serialization definition to compare with this one.</param>
        bool Equals(SerializationDefinition other)
        {
            return ProvidedByFeature() == other.ProvidedByFeature();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
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

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return ProvidedByFeature().GetHashCode();
        }

        /// <summary>
        /// The feature to enable when this serializer is selected.
        /// </summary>
        protected internal abstract Type ProvidedByFeature();

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public abstract string ContentType { get; }
    }
}