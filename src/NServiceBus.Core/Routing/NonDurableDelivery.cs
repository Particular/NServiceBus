namespace NServiceBus
{
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Instructs the transport that it's allowed to transport the message with out the need to store it durable.
    /// </summary>
    public class NonDurableDelivery : DeliveryConstraint
    {
        static  NonDurableDelivery()
        {
            RegisterDeserializer(Deserialize);
        }

        /// <inheritdoc/>
        protected override void Serialize(Dictionary<string, string> options)
        {
            options["NonDurable"] = true.ToString();
        }

        static void Deserialize(IReadOnlyDictionary<string, string> options, ICollection<DeliveryConstraint> constraints)
        {
            if (options.ContainsKey("NonDurable"))
            {
                constraints.Add(new NonDurableDelivery());
            }
        }
    }
}