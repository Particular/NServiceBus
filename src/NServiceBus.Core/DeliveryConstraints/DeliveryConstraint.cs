namespace NServiceBus.DeliveryConstraints
{
    using System.Collections.Generic;

    /// <summary>
    /// Base class for delivery constraints.
    /// </summary>
    public abstract class DeliveryConstraint
    {
        internal static List<DeliveryConstraint> EmptyConstraints = new List<DeliveryConstraint>(0);
    }
}