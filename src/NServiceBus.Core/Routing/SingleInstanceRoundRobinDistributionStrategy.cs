namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Extensibility;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public override Func<ContextBag, IEnumerable<UnicastRoutingTarget>> SelectDestination(List<UnicastRoutingTarget> allDestinations)
        {
            var distributor = new Distributor(allDestinations);
            return distributor.Distribute;
        }

        class Distributor
        {
            public Distributor(List<UnicastRoutingTarget> allDestinations)
            {
                this.allDestinations = allDestinations;
                length = allDestinations.Count;
            }

            public IEnumerable<UnicastRoutingTarget> Distribute(ContextBag bag)
            {
                SpecificInstanceHint hint;
                if (bag.TryGet(out hint))
                {
                    var result = allDestinations.FirstOrDefault(d => d.Instance != null && d.Instance.Discriminator == hint.InstanceId);
                    yield return result;
                }
                else
                {
                    var i = index % length;
                    var result = allDestinations[(int)i];
                    Interlocked.Increment(ref index);
                    yield return result;
                }
            }

            List<UnicastRoutingTarget> allDestinations;
            long index;
            long length;
        }
        
        internal class SpecificInstanceHint
        {
            public string InstanceId { get; }

            public SpecificInstanceHint(string instanceId)
            {
                InstanceId = instanceId;
            }
        }
    }
}