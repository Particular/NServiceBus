namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Manages the information about publishers.
    /// </summary>
    public class Publishers
    {
        internal IEnumerable<PublisherAddress> GetPublisherFor(Type eventType)
        {
            var distinctPublishers = rules.Select(r => r.Apply(eventType)).Where(e => e != null).Distinct().ToList();
            return distinctPublishers;
        }

        /// <summary>
        /// Registers a publisher endpoint for a given event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="publisher">The publisher endpoint.</param>
        public void Add(Type eventType, string publisher)
        {
            rules.Add(new Rule(type => StaticTypeRule(type, eventType, PublisherAddress.CreateFromEndpointName(publisher)), $"{eventType.FullName} -> {publisher}"));
        }

        /// <summary>
        /// Registers a publisher address for a given event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="publisherAddress">The publisher's physical address.</param>
        public void AddByAddress(Type eventType, string publisherAddress)
        {
            rules.Add(new Rule(type => StaticTypeRule(type, eventType, PublisherAddress.CreateFromPhysicalAddresses(publisherAddress)), $"{eventType.FullName} -> {publisherAddress}"));
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly.
        /// </summary>
        /// <param name="eventAssembly">The assembly containing the event types.</param>
        /// <param name="publisher">The publisher endpoint.</param>
        public void Add(Assembly eventAssembly, string publisher)
        {
            rules.Add(new Rule(type => StaticAssemblyRule(type, eventAssembly, null, PublisherAddress.CreateFromEndpointName(publisher)), $"{eventAssembly.GetName().Name}/* -> {publisher}"));
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly and namespace.
        /// </summary>
        /// <param name="eventAssembly">The assembly containing the event types.</param>
        /// <param name="eventNamespace">The namespace containing the event types.</param>
        /// <param name="publisher">The publisher endpoint.</param>
        public void Add(Assembly eventAssembly, string eventNamespace, string publisher)
        {
            rules.Add(new Rule(type => StaticAssemblyRule(type, eventAssembly, eventNamespace, PublisherAddress.CreateFromEndpointName(publisher)), $"{eventAssembly.GetName().Name}/{eventNamespace} -> {publisher}"));
        }

        static PublisherAddress StaticAssemblyRule(Type typeBeingQueried, Assembly configuredAssembly, string configuredNamespace, PublisherAddress configuredAddress)
        {
            return typeBeingQueried.Assembly == configuredAssembly && (configuredNamespace == null || configuredNamespace.Equals(typeBeingQueried.Namespace, StringComparison.InvariantCultureIgnoreCase))
                ? configuredAddress
                : null;
        }

        static PublisherAddress StaticTypeRule(Type typeBeingQueried, Type configuredType, PublisherAddress configuredAddress)
        {
            return typeBeingQueried == configuredType
                ? configuredAddress
                : null;
        }

        /// <summary>
        /// Adds a dynamic rule.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        /// <param name="description">Optional description of the rule.</param>
        public void AddDynamic(Func<Type, PublisherAddress> dynamicRule, string description = null)
        {
            rules.Add(new Rule(dynamicRule, description ?? "dynamic"));
        }

        List<Rule> rules = new List<Rule>();

        class Rule
        {
            public Rule(Func<Type, PublisherAddress> rule, string description)
            {
                this.rule = rule;
                this.description = description;
            }

            public PublisherAddress Apply(Type type)
            {
                return rule(type);
            }

            public override string ToString()
            {
                return description;
            }

            string description;
            Func<Type, PublisherAddress> rule;
        }
    }
}