namespace NServiceBus.Serializers.XML
{
    using System;

    [Serializable]
    public class Risk
    {
        public bool Annum { get; set; }
        public double Percent { get; set; }
        public decimal Accuracy { get; set; }
    }
}
