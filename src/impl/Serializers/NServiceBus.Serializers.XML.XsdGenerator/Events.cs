using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
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
