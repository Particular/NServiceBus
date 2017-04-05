namespace NServiceBus
{
    using System;

    sealed class ReleaseDateAttribute : Attribute
    {
        public ReleaseDateAttribute(string originalDate, string date)
        {
            OriginalDate = originalDate;
            Date = date;
        }

        public string OriginalDate { get; private set; }
        public string Date { get; private set; }
    }
}