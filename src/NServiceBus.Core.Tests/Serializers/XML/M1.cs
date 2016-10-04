namespace NServiceBus.Serializers.XML.Test
{
    using System.Collections.Generic;

    
    public class M1 : IM1
    {
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
        Dictionary<int, string> lookup_int_string = new Dictionary<int, string>();
        Dictionary<float, string> lookup_float_string = new Dictionary<float, string>();
    }
}
