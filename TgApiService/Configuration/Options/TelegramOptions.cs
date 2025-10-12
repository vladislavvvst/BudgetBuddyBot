namespace TgApiService.Configuration.Options;

internal class TelegramOptions
{
    public const string SectionName = "Telegram";
    public string Token { get; init; } = null!;
    public long AdminId { get; init; } = 0;
    public long TestUserId { get; init; } = 0;
}
