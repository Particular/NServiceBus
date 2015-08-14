namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class SatelliteCollection
    {
        readonly List<Satellite> satellites = new List<Satellite>();

        public void Register(Satellite satellite)
        {
            if (satellites.Any(x => x.Name == satellite.Name || x.ReceiveAddress == satellite.ReceiveAddress))
            {
                throw new InvalidOperationException("The given satellite name or receive address is already taken by another satellite.");
            }
            satellites.Add(satellite);
        }

        public IEnumerable<Satellite> Satellites
        {
            get { return satellites; }
        }
    }
}