namespace NServiceBus
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