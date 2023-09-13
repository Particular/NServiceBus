namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class PrerequisiteStatus
    {
        internal PrerequisiteStatus()
        {
            IsSatisfied = true;
        }

        public bool IsSatisfied { get; private set; }

        public List<string> Reasons { get; } = new();

        internal void ReportFailure(string description)
        {
            IsSatisfied = false;
            Reasons.Add(description);
        }
    }
}