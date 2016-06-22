namespace NServiceBus
{
    using System.Collections.Generic;

    class SatelliteDefinitions

    {
        public void Add(SatelliteDefinition satelliteDefinition)
        {
            satelliteDefinitions.Add(satelliteDefinition);
        }

        public List<SatelliteDefinition> Definitions => satelliteDefinitions;

        List<SatelliteDefinition> satelliteDefinitions = new List<SatelliteDefinition>();
    }
}