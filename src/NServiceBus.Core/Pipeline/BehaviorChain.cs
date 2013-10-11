namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using ObjectBuilder;

    public class BehaviorChain
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly Func<IBuilder> getBuilder;
        readonly List<BehaviorChainItemDescriptor> items = new List<BehaviorChainItemDescriptor>();

        public BehaviorChain(Func<IBuilder> getBuilder)
        {
            this.getBuilder = getBuilder;
        }

        /// <summary>
        /// Adds the given behavior to the chain
        /// </summary>
        public void Add<TBehavior>() where TBehavior : IBehavior
        {
            items.Add(new BehaviorChainItemDescriptor(typeof(TBehavior), getBuilder));
        }

        /// <summary>
        /// Adds the given behavior to the chain, allowing the caller to initialize it before it is invoked
        /// </summary>
        public void Add<TBehavior>(Action<TBehavior> init) where TBehavior : IBehavior
        {
            items.Add(new BehaviorChainItemDescriptor(typeof(TBehavior), getBuilder, init));
        }

        public void Invoke(TransportMessage incomingTransportMessage)
        {
            var head = GenerateBehaviorChain();
            
            using (var context = new BehaviorContext(incomingTransportMessage))
            {
                try
                {
                    head.Invoke(context);
                }
                catch (Exception exception)
                {
                    throw new ApplicationException(
                        string.Format("An error occurred while attempting to invoke the following behavior chain: {0}",
                                      string.Join(" -> ", items)), exception);
                }
                finally
                {
                    // todo mhg: remove
                    Console.WriteLine(context.GetTrace());

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(context.GetTrace());
                    }
                }
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine,
                               items.Select((type, idx) => new string(' ', idx * 2) + " -> " + type));
        }

        IBehavior GenerateBehaviorChain()
        {
            var clonedList = items.ToList();
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

        /// <summary>
        /// Chain item descriptor that will help create a behavior instance. The actual creation of the instance
        /// will be deferred by wrapping it in a <see cref="LazyBehavior{TBehavior}"/> which will use the builder
        /// to build the behavior when it is invoked.
        /// </summary>
        class BehaviorChainItemDescriptor
        {
            readonly Type behaviorType;
            readonly Func<IBuilder> getBuilder;
            readonly Delegate initializationMethod;

            public BehaviorChainItemDescriptor(Type behaviorType, Func<IBuilder> getBuilder, Delegate initializationMethod = null)
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
                    var instance = Activator.CreateInstance(wrapperType, new object[] {getBuilder(), initializationMethod});
                    return (IBehavior) instance;
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
    }
}