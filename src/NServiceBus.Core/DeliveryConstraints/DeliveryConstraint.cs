namespace NServiceBus.DeliveryConstraints
{
    using System.Collections.Generic;

    /// <summary>
    /// Base class for delivery constraints.
    /// </summary>
    public abstract class DeliveryConstraint
    {
        /// <summary>
        /// Serializes the constraint into the passed dictionary.
        /// </summary>
        /// <param name="options">Dictionary where to store the data.</param>
        public abstract void Serialize(Dictionary<string, string> options);
    }
}