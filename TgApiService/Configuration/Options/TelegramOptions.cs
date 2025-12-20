namespace TgApiService.Configuration.Options;

internal sealed class TelegramOptions
{
    public const string SectionName = "Telegram";
    public string Token { get; init; } = null!;
}
