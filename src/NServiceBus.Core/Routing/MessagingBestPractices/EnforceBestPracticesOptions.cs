namespace NServiceBus.Routing.MessagingBestPractices
{
    class EnforceBestPracticesOptions
    {
        public EnforceBestPracticesOptions()
        {
            Enabled = true;
        }

        public bool Enabled { get; set; }
    }
}