using System;
using System.Configuration;
using System.Collections;

namespace NServiceBus.Unicast.Config
{
    public class MessageEndpointMappingCollection : ConfigurationElementCollection
    {
        public MessageEndpointMappingCollection()
        {
            //MessageEndpointMapping mapping = (MessageEndpointMapping)CreateNewElement();

            //Add(mapping);
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return 
                    ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageEndpointMapping();
        }


        protected override ConfigurationElement CreateNewElement(string elementName)
        {
            MessageEndpointMapping result = new MessageEndpointMapping();
            result.Messages = elementName;

            return result;
        }


        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((MessageEndpointMapping)element).Messages;
        }


        public new string AddElementName
        {
            get
            { return base.AddElementName; }

            set
            { base.AddElementName = value; }

        }

        public new string ClearElementName
        {
            get
            { return base.ClearElementName; }

            set
            { base.AddElementName = value; }

        }

        public new string RemoveElementName
        {
            get
            { return base.RemoveElementName; }


        }

        public new int Count
        {
            get { return base.Count; }
        }

        public MessageEndpointMapping this[int index]
        {
            get
            {
                return (MessageEndpointMapping)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public MessageEndpointMapping this[string Name]
        {
            get
            {
                return (MessageEndpointMapping)BaseGet(Name);
            }
        }

        public int IndexOf(MessageEndpointMapping mapping)
        {
            return BaseIndexOf(mapping);
        }

        public void Add(MessageEndpointMapping mapping)
        {
            BaseAdd(mapping);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }

        public void Remove(MessageEndpointMapping mapping)
        {
            if (BaseIndexOf(mapping) >= 0)
                BaseRemove(mapping.Messages);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
