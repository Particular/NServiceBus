namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public class M3 : IMessage
    {
        public Dictionary<string, string> GenericDictionary { get; set; }

        public Hashtable Hashtable { get; set; }

        public ArrayList ArrayList { get; set; }

        public List<string> GenericList { get; set; }
    }
}