namespace NServiceBus.Pipeline
{
    class PipelineModifications
    {
        readonly RegisterStep[] additions;
        readonly RemoveStep[] removals;
        readonly ReplaceBehavior[] replacements;

        public PipelineModifications(RegisterStep[] additions, RemoveStep[] removals, ReplaceBehavior[] replacements)
        {
            this.additions = additions;
            this.removals = removals;
            this.replacements = replacements;
        }

        public RegisterStep[] Additions
        {
            get { return additions; }
        }

        public RemoveStep[] Removals
        {
            get { return removals; }
        }

        public ReplaceBehavior[] Replacements
        {
            get { return replacements; }
        }
    }
}