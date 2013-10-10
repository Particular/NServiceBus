namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;

    public class BehaviorChain
    {
        readonly Func<IBuilder> getBuilder;
        readonly List<BehaviorPipelineItem> behaviorTypes = new List<BehaviorPipelineItem>();

        class BehaviorPipelineItem
        {
            readonly Type behaviorType;
            readonly Func<IBuilder> getBuilder;
            readonly Delegate initializationMethod;

            public BehaviorPipelineItem(Type behaviorType, Func<IBuilder> getBuilder, Delegate initializationMethod = null)
            {
                this.behaviorType = behaviorType;
                this.getBuilder = getBuilder;
                this.initializationMethod = initializationMethod;
            }

            public IBehavior GetInstance()
            {
                try
                {
                    var wrapperType = typeof(LazyBehavior<>).MakeGenericType(behaviorType);
                    var instance = Activator.CreateInstance(wrapperType, new object[] { getBuilder(), initializationMethod });
                    return (IBehavior)instance;
                }
                catch (Exception exception)
                {
                    throw new ApplicationException(
                        string.Format("An error occurred while attempting to create an instance of {0} closed with {1}",
                                      typeof(LazyBehavior<>), behaviorType), exception);
                }
            }

            public override string ToString()
            {
                return behaviorType.Name;
            }
        }

        public BehaviorChain(Func<IBuilder> getBuilder)
        {
            this.getBuilder = getBuilder;
        }

        /// <summary>
        /// Adds the given behavior to the chain
        /// </summary>
        public void Add<TBehavior>() where TBehavior : IBehavior
        {
            behaviorTypes.Add(new BehaviorPipelineItem(typeof(TBehavior), getBuilder));
        }

        /// <summary>
        /// Adds the given behavior to the chain, allowing the caller to initialize it before it is invoked
        /// </summary>
        public void Add<TBehavior>(Action<TBehavior> init) where TBehavior : IBehavior
        {
            behaviorTypes.Add(new BehaviorPipelineItem(typeof(TBehavior), getBuilder, init));
        }

        public void Invoke(TransportMessage incomingTransportMessage)
        {
            var head = GenerateBehaviorChain();
            var context = new SimpleContext(incomingTransportMessage);

            try
            {
                Console.WriteLine(@"Invoking chain:
{0}", ToString());

                head.Invoke(context);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    string.Format("An error occurred while attempting to invoke the following behavior chain: {0}",
                                  string.Join(" -> ", behaviorTypes)), exception);
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine,
                               behaviorTypes.Select((type, idx) => new string(' ', idx * 2) + " -> " + type));
        }

        IBehavior GenerateBehaviorChain()
        {
            var clonedList = behaviorTypes.ToList();
            clonedList.Reverse();

            // start with the end
            IBehavior behavior = new Terminator();

            // traverse the pipeline in reverse order, tying each behavior to the following
            foreach (var type in clonedList)
            {
                var next = behavior;
                behavior = type.GetInstance();
                behavior.Next = next;
            }
            return behavior;
        }

        class SimpleContext : IBehaviorContext
        {
            public SimpleContext(TransportMessage transportMessage)
            {
                Set(transportMessage);
            }

            public TransportMessage TransportMessage
            {
                get { return Get<TransportMessage>(); }
            }

            public object Message
            {
                get { return stash["NServiceBus.Message"]; }
            }

            readonly Dictionary<string, object> stash = new Dictionary<string, object>();

            public T Get<T>()
            {
                return stash.ContainsKey(typeof(T).FullName)
                           ? (T)stash[typeof(T).FullName]
                           : default(T);
            }

            public void Set<T>(T t)
            {
                stash[typeof(T).FullName] = t;
            }
        }

        /// <summary>
        /// Special terminator behavior that eliminates the need for null checks all the way through the pipeline
        /// </summary>
        class Terminator : IBehavior
        {
            public IBehavior Next
            {
                get { throw new InvalidOperationException("Can't get next on a terminator - this behavior terminates the pipeline"); }
                set { throw new InvalidOperationException("Can't set next on a terminator - this behavior terminates the pipeline"); }
            }

            public void Invoke(IBehaviorContext context)
            {
                // noop :)
            }
        }
    }
}