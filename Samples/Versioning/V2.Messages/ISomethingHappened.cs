namespace V2.Messages
{
    public interface ISomethingHappened : V1.Messages.ISomethingHappened
    {
        string MoreInfo { get; set; }
    }
}
