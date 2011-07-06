namespace Headquarter
{
    using System;
    using Messages;
    using NServiceBus;

    public class UpdatePriceMessageHandler:IHandleMessages<UpdatePrice>
    {
        public void Handle(UpdatePrice message)
        {
            Console.WriteLine("Price update request received from the webclient");
        }
    }
}