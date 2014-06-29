namespace NServiceBus.Features
{
    using System.Collections.Generic;

    public class PrerequisiteStatus
    {
        public PrerequisiteStatus()
        {
            Reasons = new List<string>();
            IsSatisfied = true;
        }

        public bool IsSatisfied { get; private set; }

        public List<string> Reasons { get; private set; }

        internal void ReportFailure(string description)
        {
            IsSatisfied = false;
            Reasons.Add(description);
        }
    }
}