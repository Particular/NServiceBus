namespace NServiceBus
{
    using System;

    class SendRouterConnectorArguments
    {
        public string ExplicitDestination { get; set; }
        public string SpecificInstance { get; set; }

        public SendRouteOption Option
        {
            get { return option; }
            set
            {
                if (option != SendRouteOption.None)
                {
                    throw new Exception("Already specified routing option for this message: " + option);
                }
                option = value;
            }
        }

        SendRouteOption option;
    }
}