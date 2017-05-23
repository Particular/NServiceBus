namespace Particular.Licensing
{
    using System;

    sealed class ReleaseDateAttribute : Attribute
    {
        public ReleaseDateAttribute(string originalDate, string date)
        {
            OriginalDate = originalDate;
            Date = date;
        }

        public string OriginalDate { get; }
        public string Date { get; }
    }
}