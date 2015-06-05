namespace NServiceBus.Serializers.XML.XsdGenerator
{
    using System;
    using System.Collections;

    public class Element
    {
        private int minOccurs = 1;

        public string Name { get; private set; }

        public int MinOccurs
        {
            get { return minOccurs; }
        }

        public bool UnboundedMaxOccurs { get; private set; }

        public bool Nillable { get; private set; }

        public string Type { get; private set; }

        public string NameSpace { get; private set; }

        public void DoesNotNeedToOccur()
        {
            minOccurs = 0;
        }

        public void UnboundMaxOccurs()
        {
            UnboundedMaxOccurs = true;
        }

        public void MakeNillable()
        {
            Nillable = true;
        }

        private Element()
        {
            
        }

        public static Element Scan(Type t, string name)
        {
            var e = new Element();

            if (t == typeof(Guid))
            {
                Events.FoundGuid();
                e.NameSpace = "http://microsoft.com/wsdl/types/";
            }

            if (typeof(IEnumerable).IsAssignableFrom(t) || t.IsClass || t.IsInterface)
                e.DoesNotNeedToOccur();

            e.Type = Reflect.GetTypeNameFrom(t);
            e.Name = name;

            return e;
        }
    }
}
