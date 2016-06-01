namespace NServiceBus
{
    using System.Collections.Generic;

    class SatelliteDefinitions

    {
        public void Add(SatelliteDefinition satelliteDefinition)
        {
            satelliteDefinitions.Add(satelliteDefinition);
        }

        public IEnumerable<SatelliteDefinition> Definitions => satelliteDefinitions;

        List<SatelliteDefinition> satelliteDefinitions = new List<SatelliteDefinition>();
    }
}