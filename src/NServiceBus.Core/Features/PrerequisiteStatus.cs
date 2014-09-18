namespace NServiceBus.Features
{
    using System.Collections.Generic;

    /// <summary>
    /// The prerequisite status of a feature
    /// </summary>
    public class PrerequisiteStatus
    {
        internal PrerequisiteStatus()
        {
            Reasons = new List<string>();
            IsSatisfied = true;
        }

        /// <summary>
        /// True if all prerequisites for the feature is satisfied
        /// </summary>
        public bool IsSatisfied { get; private set; }

        /// <summary>
        /// The list of reason why the prerequisites are not fullfilled if applicable
        /// </summary>
        public List<string> Reasons { get; private set; }

        internal void ReportFailure(string description)
        {
            IsSatisfied = false;
            Reasons.Add(description);
        }
    }
}