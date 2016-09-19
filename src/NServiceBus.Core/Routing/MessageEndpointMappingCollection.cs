namespace NServiceBus.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Routing;
    using Routing.MessageDrivenSubscriptions;

    /// <summary>
    /// A configuration element collection of MessageEndpointMappings.
    /// </summary>
    public class MessageEndpointMappingCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Returns AddRemoveClearMap.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.AddRemoveClearMap;

        /// <summary>
        /// Calls the base AddElementName.
        /// </summary>
        public new string AddElementName
        {
            get { return base.AddElementName; }

            set { base.AddElementName = value; }
        }

        /// <summary>
        /// Calls the base ClearElementName.
        /// </summary>
        public new string ClearElementName
        {
            get { return base.ClearElementName; }

            set { base.AddElementName = value; }
        }

        /// <summary>
        /// Returns the base RemoveElementName.
        /// </summary>
        public new string RemoveElementName => base.RemoveElementName;

        /// <summary>
        /// Returns the base Count.
        /// </summary>
        public new int Count => base.Count;

        /// <summary>
        /// Gets/sets the MessageEndpointMapping at the given index.
        /// </summary>
        public MessageEndpointMapping this[int index]
        {
            get { return (MessageEndpointMapping)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Gets the MessageEndpointMapping for the given name.
        /// </summary>
        new public MessageEndpointMapping this[string Name] => (MessageEndpointMapping)BaseGet(Name);

        /// <summary>
        /// Creates a new MessageEndpointMapping.
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageEndpointMapping();
        }

        /// <summary>
        /// Creates a new MessageEndpointMapping, setting its Message property to the given name.
        /// </summary>
        protected override ConfigurationElement CreateNewElement(string elementName)
        {
            var result = new MessageEndpointMapping
            {
                Messages = elementName
            };

            return result;
        }

        /// <summary>
        /// Returns the Messages property of the given MessageEndpointMapping element.
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            var messageEndpointMapping = (MessageEndpointMapping)element;

            return $"{messageEndpointMapping.Messages}{messageEndpointMapping.AssemblyName}{messageEndpointMapping.TypeFullName}{messageEndpointMapping.Namespace}";
        }

        /// <summary>
        /// Calls BaseIndexOf on the given mapping.
        /// </summary>
        public int IndexOf(MessageEndpointMapping mapping)
        {
            return BaseIndexOf(mapping);
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(MessageEndpointMapping mapping)
        {
            BaseAdd(mapping);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        protected override void BaseAdd(ConfigurationElement element)
        {
            try
            {
                BaseAdd(element, true);
            }
            catch (ConfigurationErrorsException e)
            {
                throw new Exception($"An ambiguous message endpoint mapping has been defined at line: {e.Line}. Check the 'MessageEndpointMappings' section in {e.Filename} for conflicting mappings.", e);
            }
        }

        /// <summary>
        /// If the mapping exists, calls BaseRemove on it.
        /// </summary>
        public void Remove(MessageEndpointMapping mapping)
        {
            if (BaseIndexOf(mapping) >= 0)
            {
                BaseRemove(mapping.Messages);
            }
        }

        /// <summary>
        /// Calls BaseRemoveAt.
        /// </summary>
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        /// <summary>
        /// Calls BaseRemove.
        /// </summary>
        public void Remove(string name)
        {
            BaseRemove(name);
        }

        /// <summary>
        /// Calls BaseClear.
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }

        /// <summary>
        /// True if the collection is readonly.
        /// </summary>
        public override bool IsReadOnly()
        {
            return false;
        }

        internal void Apply(Publishers publishers, UnicastRoutingTable unicastRoutingTable, Func<string, string> makeCanonicalAddress, Conventions conventions)
        {
            var routeTableEntries = new Dictionary<Type, RouteTableEntry>();
            var publisherTableEntries = new List<PublisherTableEntry>();

            foreach (var m in this.Cast<MessageEndpointMapping>().OrderByDescending(m => m))
            {
                m.Configure((type, endpointAddress) =>
                {
                    if (!conventions.IsMessageType(type))
                    {
                        return;
                    }
                    var canonicalForm = makeCanonicalAddress(endpointAddress);
                    var baseTypes = GetBaseTypes(type, conventions);

                    RegisterMessageRoute(type, canonicalForm, routeTableEntries, baseTypes);
                    RegisterEventRoute(type, canonicalForm, publisherTableEntries, baseTypes);
                });
            }

            publishers.AddOrReplacePublishers("MessageEndpointMappings", publisherTableEntries);
            unicastRoutingTable.AddOrReplaceRoutes("MessageEndpointMappings", routeTableEntries.Values.ToList());
        }

        static void RegisterEventRoute(Type mappedType, string address, List<PublisherTableEntry> publisherTableEntries, IEnumerable<Type> baseTypes)
        {
            var publisherAddress = PublisherAddress.CreateFromPhysicalAddresses(address);
            publisherTableEntries.AddRange(baseTypes.Select(type => new PublisherTableEntry(type, publisherAddress)));
            publisherTableEntries.Add(new PublisherTableEntry(mappedType, publisherAddress));
        }

        static void RegisterMessageRoute(Type mappedType, string address, Dictionary<Type, RouteTableEntry> routeTableEntries, IEnumerable<Type> baseTypes)
        {
            var route = UnicastRoute.CreateFromPhysicalAddress(address);
            foreach (var baseType in baseTypes)
            {
                routeTableEntries[baseType] = new RouteTableEntry(baseType, route);
            }
            routeTableEntries[mappedType] = new RouteTableEntry(mappedType, route);
        }

        static List<Type> GetBaseTypes(Type messageType, Conventions conventions)
        {
            var result = new List<Type>();
            var baseType = messageType.BaseType;
            while (baseType != typeof(object) && baseType != null)
            {
                if (conventions.IsMessageType(baseType))
                {
                    if (!result.Contains(baseType))
                    {
                        result.Add(baseType);
                    }
                }

                baseType = baseType.BaseType;
            }

            foreach (var i in messageType.GetInterfaces())
            {
                if (conventions.IsMessageType(i) && !result.Contains(i))
                {
                    result.Add(i);
                }
            }

            return result;
        }
    }
}