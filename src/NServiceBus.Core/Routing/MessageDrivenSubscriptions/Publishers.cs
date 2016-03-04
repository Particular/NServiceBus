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
        /// <summary>
        /// Registers a publisher for a given endpoint type.
        /// </summary>
        /// <param name="publisher">Publisher endpoint.</param>
        /// <param name="eventType">Event type.</param>
        public void AddStatic(EndpointName publisher, Type eventType)
        {
            rules.Add(new Rule(type => StaticTypeRule(type, eventType, new PublisherAddress(publisher)), $"{eventType.FullName} -> {publisher}"));
        }

        /// <summary>
        /// Registers a publisher for a given endpoint type.
        /// </summary>
        /// <param name="publisherAddress">Publisher physical address.</param>
        /// <param name="eventType">Event type.</param>
        public void AddStatic(string publisherAddress, Type eventType)
        {
            rules.Add(new Rule(type => StaticTypeRule(type, eventType, new PublisherAddress(publisherAddress)), $"{eventType.FullName} -> {publisherAddress}"));
        }

        internal IEnumerable<PublisherAddress> GetPublisherFor(Type eventType)
        {
            var distinctPublishers = rules.Select(r => r.Apply(eventType)).Where(e => e != null).Distinct().ToList();
            return distinctPublishers;
        }

        static PublisherAddress StaticTypeRule(Type typeBeingQueried, Type configuredType, PublisherAddress configuredAddress)
        {
            return typeBeingQueried == configuredType
                ? configuredAddress
                : null;
        }

        /// <summary>
        /// Registers a publisher for all events in a given assembly (and optionally namespace).
        /// </summary>
        /// <param name="publisher">Publisher endpoint.</param>
        /// <param name="eventAssembly">Assembly containing events.</param>
        /// <param name="eventNamespace">Optional namespace containing events.</param>
        public void AddStatic(EndpointName publisher, Assembly eventAssembly, string eventNamespace = null)
        {
            rules.Add(new Rule(type => StaticAssemblyRule(type, eventAssembly, eventNamespace, new PublisherAddress(publisher)), $"{eventAssembly.GetName().Name}/{eventNamespace ?? "*"} -> {publisher}"));
        }

        static PublisherAddress StaticAssemblyRule(Type typeBeingQueried, Assembly configuredAssembly, string configuredNamespace, PublisherAddress configuredAddress)
        {
            return typeBeingQueried.Assembly == configuredAssembly && (configuredNamespace == null || configuredNamespace.Equals(typeBeingQueried.Namespace, StringComparison.InvariantCultureIgnoreCase))
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