namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents an itinerary.
    /// </summary>
    public class Itinerary
    {
        string ultimateDestination;
        string[] sendVia;

        /// <summary>
        /// Creates a new itinerary with a given ultimate destination and optional route.
        /// </summary>
        /// <param name="ultimateDestination">The ultimate destination.</param>
        /// <param name="sendVia">The route.</param>
        public Itinerary(string ultimateDestination, params string[] sendVia)
        {
            Guard.AgainstNull(nameof(ultimateDestination), ultimateDestination);
            Guard.AgainstNull(nameof(sendVia), sendVia);
            this.ultimateDestination = ultimateDestination;
            this.sendVia = sendVia;
        }

        private Itinerary()
        {
        }

        /// <summary>
        /// Checks if the itinerary is empty.
        /// </summary>
        public bool IsEmpty => ultimateDestination == null;

        /// <summary>
        /// Returns an empty itinerary.
        /// </summary>
        public static Itinerary Empty()
        {
            return new Itinerary();
        }

        /// <summary>
        /// Extracts the itinerary from the headers dictionary in a destructive way (removing the related headers from it).
        /// </summary>
        /// <param name="headers">The dictionary containing headers.</param>
        public static Itinerary ExtractFrom(IDictionary<string, string> headers)
        {
            string ultimateDestination;
            if (!headers.TryGetValue(Headers.UltimateDestination, out ultimateDestination))
            {
                return Empty();
            }
            var sendViaHeaders = headers.Where(p => p.Key.StartsWith(Headers.SendVia, StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var header in sendViaHeaders)
            {
                headers.Remove(header.Key);
            }
            var sendVia = sendViaHeaders
                .Select(p => new
                {
                    Index = int.Parse(p.Key.Replace(Headers.SendVia + ".", "")),
                    Value = p.Value
                })
                .OrderBy(x => x.Index)
                .Select(x => x.Value)
                .ToArray();
            return new Itinerary(ultimateDestination, sendVia);
        }

        /// <summary>
        /// Returns an itinerary that is one-hop advanced compared to the current one.
        /// </summary>
        /// <param name="immediateDestination"></param>
        /// <returns></returns>
        public Itinerary Advance(out string immediateDestination)
        {
            if (sendVia != null && sendVia.Length > 0)
            {
                immediateDestination = sendVia[0];
                return new Itinerary(ultimateDestination, sendVia.Skip(1).ToArray());
            }
            immediateDestination = ultimateDestination;
            return Empty();
        }

        /// <summary>
        /// Stores this itinerary in the header collection.
        /// </summary>
        public void Store(IDictionary<string, string> headers)
        {
            if (IsEmpty)
            {
                return;
            }
            headers[Headers.UltimateDestination] = ultimateDestination;
            for (var i = 0; i < sendVia.Length; i++)
            {
                var hop = sendVia[i];
                var oneBasedIndex = i + 1;
                headers[Headers.SendVia + "." + oneBasedIndex] = hop;
            }
        }
    }
}