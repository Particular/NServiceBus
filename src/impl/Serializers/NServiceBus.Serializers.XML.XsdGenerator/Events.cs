namespace NServiceBus.Serializers.XML.XsdGenerator
{
    using System;

    public static class Events
    {
        public static event EventHandler GuidDetected;
        public static void FoundGuid()
        {
            if (GuidDetected != null)
                GuidDetected(null, null);
        }
    }
}
