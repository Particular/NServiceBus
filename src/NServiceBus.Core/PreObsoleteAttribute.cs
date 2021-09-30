namespace NServiceBus
{
    using System;

    /// <summary>
    /// Meant for staging future obsoletes. Mimics the structure of <see cref="ObsoleteExAttribute"/>.
    /// </summary>
    class PreObsoleteAttribute : Attribute
    {
        public string RemoveInVersion { get; set; }
        public string TreatAsErrorFromVersion { get; set; }
        public string ReplacementTypeOrMember { get; set; }
        public string Message { get; set; }
        public string Note { get; set; }
    }
}
