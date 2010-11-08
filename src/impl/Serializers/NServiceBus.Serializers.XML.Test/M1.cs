using System;
using System.Collections.Generic;

namespace NServiceBus.Serializers.XML.Test
{
    [Serializable]
    public class M1 : IM1
    {
        public float Age { get; set; }
        public int Int { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Risk Risk { get; set; }
        public string this[int key]
        {
            get { return lookup_int_string[key]; }
            set { lookup_int_string[key] = value;}
        }

        public string this[float key]
        {
            get { return lookup_float_string[key]; }
            set { lookup_float_string[key] = value; }
        }
        private Dictionary<int, string> lookup_int_string = new Dictionary<int, string>();
        private Dictionary<float, string> lookup_float_string = new Dictionary<float, string>();
    }
}
