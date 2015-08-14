namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;

    class PipelineModificationsComposer
    {
        readonly List<PipelineModificationsBuilder> sources = new List<PipelineModificationsBuilder>();

        public void AddSource(PipelineModificationsBuilder source)
        {
            sources.Add(source);
        }

        public PipelineModifications Compose()
        {
            return new PipelineModifications(
                sources.SelectMany(x => x.Additions).ToArray(),
                sources.SelectMany(x => x.Removals).ToArray(),
                sources.SelectMany(x => x.Replacements).ToArray()
                );
        }
    }
}