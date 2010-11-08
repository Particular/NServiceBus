using System;
using System.Collections;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public class Element
    {
        private string name;
        private int minOccurs = 1;
        private bool unboundedMaxOccurs;
        private bool nillable;
        private string type;
        private string nameSpace;

        public string Name
        {
            get { return name; }
        }

        public int MinOccurs
        {
            get { return minOccurs; }
        }

        public bool UnboundedMaxOccurs
        {
            get { return unboundedMaxOccurs; }
        }

        public bool Nillable
        {
            get { return nillable; }
        }

        public string Type
        {
            get { return type; }
        }

        public string NameSpace
        {
            get { return nameSpace; }
        }

        public void DoesNotNeedToOccur()
        {
            minOccurs = 0;
        }

        public void UnboundMaxOccurs()
        {
            unboundedMaxOccurs = true;
        }

        public void MakeNillable()
        {
            nillable = true;
        }

        private Element()
        {
            
        }

        public static Element Scan(Type t, string name)
        {
            Element e = new Element();

            if (t == typeof(Guid))
            {
                Events.FoundGuid();
                e.nameSpace = "http://microsoft.com/wsdl/types/";
            }

            if (typeof(IEnumerable).IsAssignableFrom(t) || t.IsClass || t.IsInterface)
                e.DoesNotNeedToOccur();

            e.type = Reflect.GetTypeNameFrom(t);
            e.name = name;

            return e;
        }
    }
}
