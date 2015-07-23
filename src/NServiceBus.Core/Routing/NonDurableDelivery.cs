namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.DeliveryConstraints;

    /// <summary>
    /// Instructs the transport that it's allowed to transport the message with out the need to store it durable.
    /// </summary>
    public class NonDurableDelivery : DeliveryConstraint
    {
        /// <summary>
        /// Serializes the constraint into the passed dictionary.
        /// </summary>
        /// <param name="options">Dictionary where to store the data.</param>
        public override void Serialize(Dictionary<string, string> options)
        {
            options["NonDurable"] = true.ToString();
        }
    }
}