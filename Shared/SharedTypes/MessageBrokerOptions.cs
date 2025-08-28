namespace SharedTypes;

public class MessageBrokerOptions
{
    public const string MessageBroker = nameof(MessageBroker);
    public string HostName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
}
