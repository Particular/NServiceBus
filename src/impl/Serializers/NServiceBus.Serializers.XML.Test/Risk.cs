using System;

namespace NServiceBus.Serializers.XML
{
    [Serializable]
    public class Risk
    {
        public bool Annum { get; set; }
        public double Percent { get; set; }
        public decimal Accuracy { get; set; }
    }
}
