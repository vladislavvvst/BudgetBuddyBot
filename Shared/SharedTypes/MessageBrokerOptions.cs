namespace SharedTypes;

public class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";
    public string HostName { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
}
