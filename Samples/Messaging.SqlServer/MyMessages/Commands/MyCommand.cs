namespace MyMessages.Commands
{
    using NServiceBus;

    public class MyCommand:ICommand
    {
        public string Description { get; set; }
    }
}
