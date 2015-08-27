namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<RemoveStep> Removals = new List<RemoveStep>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }

    class SatellitePipelineModifications : PipelineModifications
    {
        public string Name;
        public string ReceiveAddress;

        public SatellitePipelineModifications(string name, string receiveAddress)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
        }
    }
}