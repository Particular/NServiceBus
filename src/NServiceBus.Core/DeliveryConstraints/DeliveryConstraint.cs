namespace NServiceBus.DeliveryConstraints
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for delivery constraints.
    /// </summary>
    public abstract class DeliveryConstraint
    {
        internal static List<DeliveryConstraint> EmptyConstraints = new List<DeliveryConstraint>(0);
        static List<Action<IReadOnlyDictionary<string, string>, ICollection<DeliveryConstraint>>> deserializes = new List<Action<IReadOnlyDictionary<string, string>, ICollection<DeliveryConstraint>>>();

        internal void SerializeInternal(Dictionary<string, string> options)
        {
            Serialize(options);
        }

        /// <summary>
        /// Serializes the content of the delivery constraint into the provided dictionary.
        /// </summary>
        /// <param name="options">The options dictionary.</param>
        protected virtual void Serialize(Dictionary<string, string> options)
        {
        }

        internal static List<DeliveryConstraint> DeserializeInternal(IReadOnlyDictionary<string, string> options)
        {
            var constraints = new List<DeliveryConstraint>(deserializes.Count);
            foreach (var factory in deserializes)
            {
                factory(options, constraints);
            }
            return constraints;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="factory"></param>
        /// <remarks>Not thread safe</remarks>
        protected static void RegisterDeserializer(Action<IReadOnlyDictionary<string, string>, ICollection<DeliveryConstraint>> factory)
        {
            deserializes.Add(factory);
        }
    }
}