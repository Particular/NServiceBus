namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class PrerequisiteStatus
    {
        internal PrerequisiteStatus()
        {
            Reasons = new List<string>();
            IsSatisfied = true;
        }

        public bool IsSatisfied { get; private set; }

        public List<string> Reasons { get; }

        internal void ReportFailure(string description)
        {
            IsSatisfied = false;
            Reasons.Add(description);
        }
    }
}