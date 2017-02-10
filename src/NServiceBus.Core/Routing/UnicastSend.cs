namespace NServiceBus
{
    using System;

    static partial class UnicastSend
    {
        public enum RouteOption
        {
            None,
            ExplicitDestination,
            RouteToThisInstance,
            RouteToAnyInstanceOfThisEndpoint,
            RouteToSpecificInstance
        }
        
        public class State
        {
            public string ExplicitDestination { get; set; }
            public string SpecificInstance { get; set; }

            public RouteOption Option
            {
                get { return option; }
                set
                {
                    if (option != RouteOption.None)
                    {
                        throw new Exception("Already specified routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            RouteOption option;
        }
    }
}