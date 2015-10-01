namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.DeliveryConstraints;

    /// <summary>
    /// A collection of delivery constraints.
    /// </summary>
    public class DeliveryConstraintCollection
    {
        List<DeliveryConstraint> constraints;

        internal DeliveryConstraintCollection(IEnumerable<DeliveryConstraint> constraints)
        {
            this.constraints = constraints.ToList();
        }

        /// <summary>
        /// Tries to get and remove constraint of a given type.
        /// </summary>
        /// <typeparam name="T">Requested type.</typeparam>
        /// <param name="constraint">Returned constraint or null.</param>
        /// <returns>Success or failure</returns>
        public bool TryRemove<T>(out T constraint) where T : DeliveryConstraint
        {
            constraint = constraints.OfType<T>().SingleOrDefault();
            if (constraint != null)
            {
                constraints.Remove(constraint);
            }
            return constraint != null;
        }

        internal void RaiseErrorIfNotAllConstrainstHaveBeenHandled()
        {
            if (constraints.Count > 0)
            {
                throw new Exception("Following delivery constraints have not been handled: " + string.Join(", ", constraints.Select(c => c.GetType().Name)));
            }
        }
    }
}