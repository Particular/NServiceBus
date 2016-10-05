namespace NServiceBus.Hosting.Helpers
{
    /// <summary>
    /// Contains information about a file that was skipped during scanning along with a text describing
    /// the reason why the file was skipped.
    /// </summary>
    public class SkippedFile
    {
        internal SkippedFile(string filePath, string message)
        {
            FilePath = filePath;
            SkipReason = message;
        }

        /// <summary>
        /// The full path to the file that was skipped.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Description of the reason why this file was skipped.
        /// </summary>
        public string SkipReason { get; private set; }
    }
}